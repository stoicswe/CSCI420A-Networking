{-# LANGUAGE ViewPatterns      #-}
{-# LANGUAGE OverloadedStrings #-}
{-# LANGUAGE TemplateHaskell   #-}
{-# LANGUAGE TypeApplications  #-}
module Frame 
    ( Frame(..)
    , opcode
    , fin
    , mask
    , readFrame
    ) where

import           Control.Concurrent (forkFinally)
import           Control.Concurrent.STM
import           Control.Exception (bracket)
import           Control.Monad (unless, forever, void, when)
import qualified Data.Map as M
import qualified Data.ByteString as B
import qualified Data.ByteString.Base64 as B64
import qualified Data.ByteString.Base16 as B16
import qualified Data.Text as T
import           Data.List (dropWhileEnd)
import           Data.List.Split (splitOn)
import           Data.Maybe (mapMaybe)
import           Data.Char (isSpace, chr)
import qualified Data.Text.Encoding as E
import           Numeric (readHex)
import           Network.Socket 
import           Network.Socket.ByteString
import           Control.Lens
import qualified Crypto.Hash.SHA1 as SHA1
import           System.IO
import           Data.Word
import           Data.Bits.Lens
import           Data.Bits

data Frame
    = Frame
    { _fin  :: !Bool
    , _rsv1 :: !Bool
    , _rsv2 :: !Bool
    , _rsv3 :: !Bool
    , _opcode :: Word8
    , _mask :: [Word8]
    , _payloadLength :: Int
    , _payload :: B.ByteString
    }
    deriving (Eq, Show)

makeLenses ''Frame

emptyFrame :: Frame
emptyFrame = Frame False False False False 0 [0] 0 B.empty

readFrame :: Handle -> IO Frame
readFrame h = do
    a <- readWord @Word16 16 h
    let f = emptyFrame 
              & fin  .~ a^.bitAt 15
              & rsv1 .~ a^.bitAt 14
              & rsv2 .~ a^.bitAt 13
              & rsv3 .~ a^.bitAt 12
              & opcode .~ bitRange 8 11 a
        maskBit = a^.bitAt 7
        len = bitRange 0 6 a
    print ("frame", a, f, maskBit, len)
    len' <- readLen h len
    print ("len'", len')
    m  <- if maskBit then B.unpack <$> B.hGet h 4 else return [0] 
    print ("m", m)
    bs <- B.hGet h len'
    print ("bs", T.unpack . E.decodeUtf8 . B16.encode $ bs)
    return $ f & mask .~ m 
               & payloadLength .~ len'
               & payload .~ (if maskBit then unmask m bs else bs)

unmask :: [Word8] -> B.ByteString -> B.ByteString
unmask m bs = B.pack . zipWith xor (cycle m) . B.unpack $ bs

readLen :: Handle -> Int -> IO Int
readLen h l
   | l == 126  = readWord 16 h
   | l == 127  = readWord 64 h
   | otherwise = return l

bitRange :: (Integral a, Bits a, Integral b, Bits b) => Int -> Int -> b -> a
bitRange l h v = fromIntegral (v `shiftR` l) .&. ((1 `shiftL` (h-l+1)) - 1)

readWord :: (Integral a, Bits a) => Int -> Handle -> IO a
readWord n h = B.foldl' (\a w -> fromIntegral w .|. (a `shiftL` 8)) 0 <$> B.hGet h (n `shiftR` 3) 
