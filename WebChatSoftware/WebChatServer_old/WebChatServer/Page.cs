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
        .chat {
            width: 300px;
        }

        .message,.echo {
            width: 200px;
            padding: 10px;
            margin-bottom: 10px;
            border-radius: 10px;
        }

        .message {
            background: #aaf;
            margin-right: auto;
            background-image: var(--my-url);
            --my-url: url(https://www.gravatar.com/avatar/5d8d81116011a01424b4dbe952846c25)
        }

        .time-right {
            float: right;
            color: #aaa;
        }       

        .echo {
            background: #afa;
            margin-left: auto;
            background-image: var(--my-url);
            --my-url: url(https://www.gravatar.com/avatar/a7e7032c2af3eb67320f5d19354d2499)
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

        function addMessage(s, source) {
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
            newMessaage(s)
        }

        function clicked() {
            addMessage(document.getElementById('message').value,'echo')
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
        <input type='button' onclick='clicked()'>
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
            log(evt.data);
            var msg = evt.data.split('::')[2];
            addMessage(msg, 'message');
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
