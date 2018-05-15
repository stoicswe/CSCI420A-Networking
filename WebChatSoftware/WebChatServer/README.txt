About
---------------

FLOPS is a website hosting server software, primarily focused on webchat and websocket interfaces.

How to Install (Build from source)

Dependances
---------------

.NET Core SDK / .NET Core Runtime
https://github.com/dotnet/core/blob/master/release-notes/download-archives/2.0.6-download.md

How to Build / Install
-------------------------

In a CMD or Terminal window, within the working directory of the project ( ./WebChatServer/ ):

dotnet run

then you are good to go!

How to Configure
--------------------

This software does in faact have an I/O system for writing to the hard disk, however, it has not yet
been implemented and if this project is further developed, a configuration file will be developed.

Deficiencies
------------------------
The server is currently hard-coded to make a simple website. The server does not save any website sessions
to the hard drive. Anything saved into it will be deleted upon closing the server. Feel free to modify this
code if you wish.