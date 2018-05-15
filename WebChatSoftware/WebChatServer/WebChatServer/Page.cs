using System;
using System.Collections.Generic;
using System.Text;

namespace WebChatServer
{
    class Page
    {
/*static string pageToSend = @"
    < !DOCTYPE html>
    <html lang = 'en' >
    < head >
    < meta charset='UTF-8'>
    <meta name = 'viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='X-UA-Compatible' content='ie=edge'>
    <title>Document</title>
    </head>
    <body>
    <div id = 'log' ></ div >
    < script >
    logE = function() { return document.getElementById('log'); };
    log = function(s)
    {
        var p = document.createElement('p');
        p.appendChild(document.createTextNode(s));
        logE().appendChild(p);
    };

    var ws = new WebSocket('ws://' + window.location.host + '/ws', ['issues']);
    ws.onopen = function(e) { log('Connection Open.'); };
    ws.onmessage = function(evt) { log(evt.data); };
    ws.onclose = function(evt) { log('Connection closed.'); };
    function newIssue(e)
    {
        if (e.preventDefault) e.preventDefault();
        ws.send('new: ' + document.getElementById(wsissue).value);
        return false
    }
    </script>
    </body>
    </html>
";
*/
        static string pageToSend = @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <title>Chat Javascript side.</title>
    <style lang='text/css'>

        body{
            font-family: sans serif;
        }

        #log {
            position: absolute;
            top: 0;
        }

        .chat {
            height: 90vh;
            width: 50vh;
            margin-top: 5vh;
            margin-left: auto;
            margin-right: auto;
            overflow-y: scroll;
            overflow-x: hidden;
            color: white;
        }

        .message,.echo {
            width: 200px;
            padding: 10px;
            margin-bottom: 10px;
            border-radius: 10px;
            box-shadow: 4px 4px 15px rgba(155,155,155,0.6);
            overflow-x: hidden;
            animation-name: zoom;
            animation-duration: 300ms;
            animation-timing-function: ease-out;
        }

        @keyframes zoom {
            from {
                width: 0px;
                padding: 0px;
            }
            to {
                width: 200px;
                padding: 10px;
            }
        }

        .message {
            color: black;
            margin-right: auto;
            background: var(--my-url);
            --my-url: #c4c4c4;
            transition: padding 200ms;
        }

        .message:hover {
            padding: 20px;
            box-shadow: 4px 4px 15px rgba(155,155,155,0.6);
        }

        .time-right {
            float: right;
            color: #fff;
        }       

        .echo {
            margin-left: auto;
            background: var(--my-url);
            --my-url: #2272f4;
            transition: padding 200ms;
        }

        .echo:hover {
            padding: 20px;
            box-shadow: 4px 4px 15px rgba(155,155,155,0.6);
        }

        .talk {
            margin-left: auto;
            margin-right: auto;
            width: 55vh;
        }

        #message {
            width:70%;
            border-radius: 10px;
            border-style: solid;
            border-width: 1px;
            box-shadow: 4px 4px 15px rgba(155,155,155,0.6);
            transition: box-shadow 200ms;
            padding: 2px;
            transition: padding 200ms;
        }

        #message:hover {
            box-shadow: 8px 8px 20px rgba(155,155,155,0.6);
            padding: 4px;
        }

        #send {
            width: 15%;
            border-radius: 10px;
            border-style: solid;
            border-width: 1px;
            background: #2272f4;
            box-shadow: 4px 4px 15px rgba(155,155,155,0.6);
            color: white;
            transition: width 200ms;
        }

        #send:hover {
            width: 20%
        }
    </style>
    <script lang='text/javascript'>

        var ws = '';

        function newMessaage(msg) {
            
            ws.send('echo::ALL::' + msg);
        }

        logE = function() { return document.getElementById('log'); };
        log = function(s)
        {
            var p = document.createElement('p');
            p.appendChild(document.createTextNode(s));
            logE().appendChild(p);
        };

        function addMessage(s, source, send) {
            if(s){
                c = document.getElementById('chat1')
                d = document.createElement('div')
                time = document.createElement('span')
                time.classList.add('time-right')
                var tn = new Date();
                var nh = tn.getHours();
                var nm = tn.getMinutes();
                timeValue = document.createTextNode(nh+ ':' +nm )
                time.appendChild(timeValue)
                d.classList.add(source)
                t = document.createTextNode(s)
                d.appendChild(t)
                d.appendChild(time)
                c.appendChild(d)
                c.scrollTop = c.scrollHeight;
                if(send){
                    newMessaage(s)
                }
            }
        }

        function clicked() {
            addMessage(document.getElementById('message').value,'echo', true)
            document.getElementById('message').value = ''  
        }

        function message_keydown(event) {
            var key = event.keyCode
            if (key == 13)
                clicked()
            // document.getElementById('demo').innerHTML = 'Unicode CHARACTER code: ' + key;
        }
    </script>
</head>
<body>
    <div id='chat1' class='chat'>
    </div>
    <div class='talk'>
        <input id='message' type='text' onkeydown='message_keydown(event)'>
        <input id='send' type='button' value='Send' onclick='clicked()'>
    </div>
    <!-- <div class='message' style='--my-url: url(https://www.gravatar.com/avatar/5d8d81116011a01424b4dbe952846c25)'>
aa;lskdjalksdj
    </div> -->
    <div id='demo'></div>
    <div id = 'log' ></ div >
     <script>
        ws = new WebSocket('ws://' + window.location.host + '/ws', ['Chatting']);
        ws.onopen = function(e) { log('Connection Open.'); };
        ws.onmessage = function(evt) { 
            //log(evt.data);
            var msg = evt.data.split('::')[2];
            addMessage(msg, 'message', false);
        };
        ws.onclose = function(evt) { log('Connection closed.'); };
        ws.onerror = function(error) {console.log('WebSocket Error: ' + error);};
    </script>
</body>
</html>";

        public static string getPage()
        {
            return pageToSend;
        }
    }
}
