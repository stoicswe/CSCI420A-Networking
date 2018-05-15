{-# LANGUAGE ViewPatterns      #-}
{-# LANGUAGE OverloadedStrings #-}
{-# LANGUAGE TemplateHaskell   #-}
import           Control.Concurrent (forkFinally)
import           Control.Concurrent.STM
import           Control.Exception (bracket)
import           Control.Monad (unless, forever, void, when)
import qualified Data.Map as M
import qualified Data.ByteString as B
import qualified Data.ByteString.Lazy as BL
import qualified Data.ByteString.Base64 as B64
import qualified Data.ByteString.Base16 as B16
import qualified Data.Text as T
import           Data.List (dropWhileEnd)
import           Data.List.Split (splitOn)
import           Data.Maybe (mapMaybe, fromMaybe)
import           Data.Monoid
import           Data.Char (isSpace, chr)
import qualified Data.Text.Encoding as E
import           Numeric (readHex)
import           Network.Socket 
import           Network.Socket.ByteString
import           Control.Lens
import qualified Crypto.Hash.SHA1 as SHA1
import           System.IO
import           Frame
import           Lucid
import           Lucid.Html5

type Url = String

data IssueStatus = New | Fixed | WontFix | Duplicate | Assigned
    deriving (Eq, Show, Read, Enum, Bounded)

allStatus :: [IssueStatus]
allStatus = enumFromTo minBound maxBound

data Issue 
    = Issue
        { _number :: Int
        , _desc   :: T.Text
        , _status :: IssueStatus
        }
    deriving (Eq, Show)

emptyIssue = Issue 0 "" New

makeLenses ''Issue

newtype IssueData = IssueData (TVar (Int, M.Map Int Issue))

parseUrl = Right

trim = dropWhileEnd isSpace . dropWhile isSpace

data HttpRequest 
  = Get  { _host :: String, _version :: String, _wsKey :: String,           _url :: Url }
  | Head { _host :: String, _version :: String,                             _url :: Url }
  | Post { _host :: String, _version :: String
         , _contentLength :: Int, _args :: [(String,String)], _url :: Url }

makeLenses ''HttpRequest

isPost :: HttpRequest -> Bool
isPost Post{} = True
isPost _      = False

parseHeader :: [String] -> Either String HttpRequest 
parseHeader (h:hs) =
    case words h of
      ["GET",  url, version] -> Get  "" ""   "" <$> parseUrl url >>= parseRest hs
      ["HEAD", url, version] -> Head "" ""      <$> parseUrl url >>= parseRest hs
      ["POST", url, version] -> Post "" "" 0 [] <$> parseUrl url >>= parseRest hs
      _                      -> Left $ "Expected GET, HEAD, or POST, saw:\n" ++ h
  where
    parseRest []     r = Right r
    parseRest (h:hs) r =
        case words h of
          ["Host:", n] -> parseRest hs (r & host .~ n)
          ["Sec-WebSocket-Key:", key] -> parseRest hs (r & wsKey .~ key)
          ["Content-Length:", reads -> [(n, "")]]
            | isPost r -> parseRest hs (r & contentLength .~ n)
          _            -> parseRest hs r

splitAtChar :: Char -> String -> Maybe (String, String)
splitAtChar c = go ""
  where
    go as [] = Nothing
    go as (s:ss)
      | s == c    = Just (reverse as, ss)
      | otherwise = go (s:as) ss

readPost :: Handle -> HttpRequest -> IO HttpRequest
readPost h r@Post{} = do
    as <- mapMaybe (splitAtChar '=') . splitOn "&"
            <$> readLength h (fromMaybe 0 (r^?contentLength))
    return (r & args .~ as)
readPost _ _ = error "Not a post"

readLength :: Handle -> Int -> IO String
readLength h l = T.unpack . E.decodeUtf8 <$> B.hGet h (min l 4096)

main :: IO ()
main = do
    issues <- IssueData <$> newTVarIO (0, M.empty)
    addr <- resolve "8080"
    bracket (open addr) close (loop issues)
  where
    resolve port = do
        let hints = defaultHints {
                addrFlags = [AI_PASSIVE]
              , addrSocketType = Stream
              }
        addr:_ <- getAddrInfo (Just hints) Nothing (Just port)
        return addr
    open addr = do
        sock <- socket (addrFamily addr) (addrSocketType addr) (addrProtocol addr)
        setSocketOption sock ReuseAddr 1
        bind sock (addrAddress addr)
        listen sock 10
        return sock
    loop issues sock = forever $ do
        (conn, peer) <- accept sock
        putStrLn $ "Connection from " ++ show peer
        h <- socketToHandle conn ReadWriteMode 
        void $ forkFinally (talk issues h) (\_ -> hClose h)
    talk issues h = do
        header <- getHeader h
        let r = parseHeader header
        case r of
            Right req ->
                case req of
                    Get  host version   ""   url   -> print ("GET",  host, url,    version) >> sendUrl issues h url
                    Get  host version   key  "/ws" -> print ("GET",  host, "/ws",  version) >> startWS issues key h
                    Head host version        url   -> print ("HEAD", host, url,    version) >> sendHead h url
                    Post host version l args url   -> print ("POST", host, url, l, version, args) 
                                                      >> readPost h req >>= handlePost issues h
            Left s    -> putStrLn s >> send404 h
    getHeader h = do
        l <- trim <$> hGetLine h
        print l
        if null l
            then return []
            else (l:) <$> getHeader h

startWS :: IssueData -> String -> Handle -> IO ()
startWS (IssueData issues) key conn = B.hPut conn (E.encodeUtf8 msg) >> loop
  where
    msg = T.unlines
            [ "HTTP/1.1 101 Switching Protocols"
            , "Upgrade: websocket"
            , "Connection: Upgrade"
            , "Sec-WebSocket-Accept: " <> token
            -- If we wanted to support things like compression we would state that
            -- as an extension here.
            -- , "Sec-WebSocket-Extensions:"
            , "Sec-WebSocket-Protocol: issues"
            , ""
            ]
    token = E.decodeUtf8 . B64.encode . SHA1.hash . E.encodeUtf8 . T.pack $ key ++ guid
    guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
    loop = do
        f <- readFrame conn
        print ("ws frame", f)
        when (f^.opcode /= 8) loop

send404 :: Handle -> IO ()
send404 conn = logM "sending 404 header" >> B.hPut conn (E.encodeUtf8 msg)
  where
    msg = T.unlines
            [ "HTTP/1.0 404 File not found"
            , "Server: CSCI420/2018 Haskell/8.0.2"
            , "Connection: close"
            , "Content-type: text/html; charset=utf-8"
            , ""
            ]

send303 :: Handle -> IO ()
send303 conn = B.hPut conn (E.encodeUtf8 msg)
  where
    msg = T.unlines
            [ "HTTP/1.1 303 See Other"
            , "Location: /"
            , ""
            ]

logM = putStrLn

sendHead :: Handle -> Url -> IO ()
sendHead conn url = logM "sending 200 header" >> B.hPut conn (E.encodeUtf8 msg)
  where
    msg = T.unlines
            [ "HTTP/1.0 200 OK"
            , "Server: CSCI420/2018 Haskell/8.0.2"
            , "Content-type: text/html; charset=utf-8"
            , ""
            ]

sendUrl :: IssueData -> Handle -> Url -> IO ()
sendUrl _ conn "/me.js" = do -- this is something i added, with much help
    rs <- B.readFile "myfile.js" 
    sendHead conn ""
    B.hPut conn rs
sendUrl (IssueData issues) conn url = do
    (n,m) <- atomically $ readTVar issues
    sendHead conn url
    logM "sending html"
    BL.hPut conn (renderBS (html m))
  where
    html m = doctypehtml_ $ do
                head_ $ do
                    title_ "Welcome!"
                    style_ cssStyle
                    script_ script
                body_ $ do
                    h3_ "A simple and broken Haskell issue tracking website."
                    formatIssues m
                    form_ [action_ "/", method_ "post"] $ do
                        label_ [for_ "issue"] "New Issue:"
                        br_ []
                        textarea_ [name_ "issue", cols_ "50", rows_ "10"] ""
                        br_ []
                        input_ [type_ "submit", value_ "submit"]
                    div_ [id_ "log"] ""

formatIssues :: M.Map Int Issue -> Html ()
formatIssues m = table_ [] . mconcat $ 
                    [ tr_ $ do
                        td_ $ i^.number.to (toHtml.show)
                        td_ $ toHtmlRaw $ decodeURI $ i^.desc
                        td_ $ statusForm (i^.number.to (T.pack.show)) (i^.status)
                    | (n, i) <- M.toList m
                    ]
  where
    decodeURI = decodeURILocalT 
    statusForm :: T.Text -> IssueStatus -> Html ()
    statusForm i s =
        form_ [action_ "/", method_ "post"] $ do
            select_ [onchange_ "this.form.submit()", name_ "status"]
                (mapM_ (option s) $ allStatus)
            input_ [type_ "hidden", name_ "index", value_ i]
            input_ [type_ "submit", value_ "update"]
    option :: IssueStatus -> IssueStatus -> Html ()
    option active s@(T.pack . show -> t) =
        option_ (value_ t : if s == active then [selected_ T.empty] else []) (toHtml t)

decodeURILocalT :: T.Text -> T.Text
decodeURILocalT (T.unpack -> s) = T.pack . decodeURILocal $ s

decodeURILocal :: String -> String
decodeURILocal = g . f
  where
    f [] = []
    f ('+':cs) = ' ' : f cs
    f ('%':a:b:cs) | [(n,"")] <- readHex [a,b] = chr n : f cs
    f (c:cs) = c : f cs

    g [] = []
    g ('<' :cs) = "&lt;"   ++ g cs
    g ('>' :cs) = "&gt;"   ++ g cs
    g ('&' :cs) = "&amp;"  ++ g cs
    g ('"' :cs) = "&quot;" ++ g cs
    g ('\'':cs) = "&apos;" ++ g cs
    g ('\n':cs) = "<br>"   ++ g cs
    g (c   :cs) = c : g cs

cssStyle :: T.Text
cssStyle = T.unlines
    [ "body { font-family: sans-serif }"
    , "table { border: 3px solid #CCF; }"
    , "td { padding: 10px; }"
    , "td:nth-child(1)    {width: 64px; text-align: center}"
    , "td:nth-child(2)    {width: 400px;}"
    , "tr:nth-child(even) {background: #CFC}"
    , "tr:nth-child(odd)  {background: #FFF}"
    ]

script :: T.Text
script = T.unlines
    [ "logE = function() { return document.getElementById('log'); };"
    , "log = function(s) {"
    , "  var p = document.createElement('p');"
    , "  p.appendChild(document.createTextNode(s));"
    , "  logE().appendChild(p);"
    , "};"
    , "var ws = new WebSocket('ws://' + window.location.host + '/ws', ['issues']);"
    , "ws.onopen = function(e) { log('Connection Open.'); };"
    , "ws.onmessage = function(evt) { log(evt.data); };"
    , "ws.onclose = function(evt) { log('Connection closed.'); };"
    , "function newIssue(e) {"
    , "  if (e.preventDefault) e.preventDefault();"
    , "  ws.send(\"new: \" + document.getElementById(wsissue).value);"
    , "  return false;"
    , "}"
    ]
                
handlePost :: IssueData -> Handle -> HttpRequest -> IO ()
handlePost issues conn r@Post{} = do
        b <- updateState issues (r^.args)
        if b
            then send303 conn
            else send404 conn
handlePost _      conn _ = send404 conn

whenRead :: (Monad m, Read r) => String -> (r -> m Bool)-> m Bool
whenRead i act
  | [(n, "")] <- reads i = act n
  | otherwise            = return False

updateState :: IssueData -> [(String, String)] -> IO Bool
updateState issues [("status", decodeURILocal -> s), ("index", i)]
     = whenRead s $ \s' ->
         whenRead i $ \i' -> do
           print ("updating", s', i')
           updateStatus issues s' i'
updateState (IssueData issues) [("issue", i)] = do
    let issue = emptyIssue & desc .~ T.pack i
    atomically $ do
        (n,m) <- readTVar issues
        writeTVar issues (n+1, M.insert n (issue & number .~ n) m)
    print i
    return True
updateState _ bs = do
    print bs
    return False

updateStatus :: IssueData -> IssueStatus -> Int -> IO Bool
updateStatus (IssueData issues) s i = atomically $ modifyTVar' issues (_2 %~ M.adjust (status .~ s) i) >> return True