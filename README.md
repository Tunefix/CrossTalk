# CrossTalk
Audio Communications Software

CrossTalk is based around "loops", where each client can choose to listen and/or talk to different loops.
All cross-mixing is done in the server software so each client only has a single audio stream to and from the server.
If a client is both talking and listening to the same loop, they will not get the audio from themself back.

On the server you can set the individual names of the loops, and the names will be pushed out to the clients. This can be done at any time.

The client supports system-wide PTT (Push-To-Talk) using a keyboard-button of your choosing.

### SCREENSHOTS

![alt tag](https://i.imgur.com/WjHpB0H.png)
Client Screenshot

![alt tag](https://i.imgur.com/Y8WqerR.png)
Server Screenshot

### GETTING STARTED
Run the server, and make sure the server-pc gets traffic on UPD-port 32123.
Start the client(s), type in the ip/url to the server and click "connect".

### AUDIO TRANSMISSION FORMATS
* PCM 32bit float 48kHz (3000 kbit/s)
* G722 48kHz stereo (384 kbit/s)
* G722 24kHz mono (96 kbit/s)
* G722 16kHz mono (64 kbit/s)
* OPUS 128 kbit/s stereo
* OPUS 64 kbit/s stereo
* OPUS 128 kbit/s mono
* OPUS 64 kbit/s mono
* OPUS 32 kbit/s mono
* OPUS 16 kbit/s mono

**Note:** Remeber that all audio is first encoded on each client before being sent to the server. On the server it is being decoded, mixed and encoded again before being sent back. On the client it is then decoded. This means that all audio is being coded twice, and this will have adverse effects on the sound quality, especially if using havy compression i.e. low bitrates. It is therefore recommended to use a high quality codec, such as OPUS 128 kbit/s mono.

**Note 2:** The PCM 32bit float 48Khz format uses a _HUGE_ amount of bandwith, and should only be used when on a local network (LAN).

**Note on the difference between OPUS and G722:**
Some might wonder why I have the G722-codec in here. It has one big advantage over OPUS, and that is it will not "bubble". Instead the frequency-response gets lower as bandwith is lowered. (Bandwith is a function of the input sample-rate.)
