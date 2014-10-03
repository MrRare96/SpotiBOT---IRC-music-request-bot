using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO; // for reading/writing to the server
using System.Net.Sockets; // Connecting to server
using System.Net; // for using WebClient
using System.Xml;//for db ish stuff
using System.Data; // dunno
using System.Xml.Linq;// xml stuff
using System.Diagnostics;




namespace ConsoleApplication2.IrcBot
{



    class IrcBot
    {


   

        //the irc connection/retrieving/sending code part is from http://www.hackforums.net/printthread.php?tid=283345, but i try to understand it, commands are done by me, and what i think something does.
		//all other stuff about controlling spotify and such is by me :)
       
		//here I start the 3 base programs at the same time(no more a kazilion cmd windows popping up :P)
        static void Main()
        {

            //System.Threading.Thread.Sleep(5000);

           
            

            Parallel.Invoke(
                () => spotibot(),
                () => MusicPlayer(),
                () => checkdeb());

           
            
        }

		//its been a long time since seeing this, this retrieves the song data from the url input retrieved from the irc chat 
        private static string[] saveSongData(string url)
        {
            

            if (url.Contains("youtube"))
            {
                string[] songdata = getSongData(url, "<meta itemprop=\"name\" content=\"", "not available", "\"length_seconds\":", "\">", "noartist", ",");
                return songdata;
            }
            else if (url.Contains(".last.fm/music"))
            {

                WebClient client2 = new WebClient();//starts new webclient on var client
                client2.Headers.Add("user-agent", "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.1.13) Gecko/20080311 Firefox/2.0.0.13");
                string data = client2.DownloadString(url);//download data from given url

                string yturlsearch = "data-youtube-player-id=\"";
                string artistsearch = "<h3 title=\"";
                string artistsearchend = "\">";

                int posyturl = data.IndexOf(yturlsearch);
                string yturlunref = data.Substring(posyturl, 35);
                string yturl = yturlunref.Replace(yturlsearch, "");

                int posartist = data.IndexOf(artistsearch);
                string artistunref = data.Substring(posartist, 150);
                int posartistend = artistunref.IndexOf(artistsearchend);
                string artistunref2 = data.Substring(posartist, posartistend);
                string artist = artistunref2.Replace(artistsearch, "");

                string[] songdata = getSongData("https://www.youtube.com/watch?v=" + yturl, "<meta itemprop=\"name\" content=\"", artist, "\"length_seconds\":", "\">", "noartist", ",");
                return songdata;

            }
            else if (url.Contains("spotify:"))
            {
                string[] songdata = addsong(url);
                return songdata;
            }
            else
            {
                string[] songdata = new string[] { "no", "songinfo" };
                return songdata;
            }

           
        }

		//my awesome naming makes this so confusing XD, im to afraid to change it now, but this sorts the songdata to be send to the xml creating fuction thing
        private static string[] getSongData(string url, string songnamesearch, string artistsearch, string durationsearch, string songnameend, string artistend, string durationend)
        {
            string artistesc;
            WebClient client2 = new WebClient();//starts new webclient on var client
            client2.Headers.Add("user-agent", "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.1.13) Gecko/20080311 Firefox/2.0.0.13");
            string data = client2.DownloadString(url);//download data from given url

            int possongname = data.IndexOf(songnamesearch);
            string songnameunref = data.Substring(possongname, 150);
            int possongname_end = songnameunref.IndexOf(songnameend);
            string songnameunref2 = data.Substring(possongname, possongname_end);
            string songname = songnameunref2.Replace(songnamesearch, "");

            if (url.Contains("youtube"))
            {
                artistesc = artistsearch;
            }
            else
            {

                int posartist = data.IndexOf(artistsearch);
                string artistunref = data.Substring(posartist, 150);
                int posartist_end = artistunref.IndexOf(artistend);
                string artistunref2 = data.Substring(posartist, posartist_end);
                string artist = artistunref2.Replace(artistsearch, "");

                int poscommaartist = artist.IndexOf("&#039;");
               

                if (poscommaartist > 1)
                {
                    artistesc = artist.Replace("&#039;", "\'");
                }
                else
                {
                    artistesc = artist;
                }

            }

            int posduration = data.IndexOf(durationsearch);
            string durationunref = data.Substring(posduration, 150);
            int posduration_end = durationunref.IndexOf(durationend);
            string durationunref2 = data.Substring(posduration, posduration_end);
            string duration = durationunref2.Replace(durationsearch, "");

            int poscommasongname = songname.IndexOf("&#039;");
            

    
            string songnameesc;
            

            if (poscommasongname > 1)
            {
                songnameesc = songname.Replace("&#039;", "\'");
            }
            else
            {
                songnameesc = songname;
            }

            url = url.Replace("https://www.youtube.com/watch?v=", "");

            string adddata = addxml(songnameesc, artistesc, duration, url, "1");





            string[] songinfo = new string[] { songnameesc, artistesc, duration, adddata };

            return songinfo;
     
     


        }

		//to actual pause/control the (spotify) player it needs to be done in a browser, therefor a html page is created
        private static void createPlayer(string uri, int time)
        {


            string htmlpage = "MusicPlayer.html";
            string htmldata;

            if (uri.Length == 11)
            {
                htmldata = "<html><head> </head><body> <script src=\"http://www.youtube.com/player_api\"></script> <script type=\"text/javascript\" LANGUAGE=\"JavaScript\">; var player; function onYouTubePlayerAPIReady() { player = new YT.Player('player', { height: '390', width: '640', videoId: '" + uri + "', events: { 'onReady': onPlayerReady, 'onStateChange': onPlayerStateChange } }); }  function onPlayerReady(event) { event.target.playVideo(); } function onPlayerStateChange(event) { if(event.data === 0) { open(\"MusicPlayer.html\", '_self').close(); } if(event.data == YT.PlayerState.PAUSED){ open(\"paused.html\", '_blank').close(); } if(event.data == YT.PlayerState.PLAYING){ open(\"playing.html\", '_blank').close(); } } </script> <div id=\"player\"> </div> </body></html>";
                System.IO.File.WriteAllText(htmlpage, htmldata);
            }
            else if (uri.Contains("spotify"))
            {
                htmldata = "<html><body><script>setTimeout(\"window.close()\", " + time * 1000 + ");</script><iframe src=\"https://embed.spotify.com/?uri=" + uri + "\" name=\"MusicPlayer\" allowTransparency=\"true\" scrolling=\"no\" frameborder=\"0\" ></iframe></body></html>";
                System.IO.File.WriteAllText(htmlpage, htmldata);
            }
            
            
            


        }

		//starts the song, using all kinds of cmd commands to lauch the html player and start spotify
        private static void MusicLauncher(string uri, int timer)
        {
            createPlayer(uri, timer);

            string chromepad = @"C:\Program Files (x86)\Google\Chrome\Application\";
            string curDirtest = Directory.GetCurrentDirectory();
            string curDirtest2 = curDirtest.Replace("\\", "/");
            string curDir = curDirtest.Replace(" ", "%20");
            string website = "file:///" + curDir + "/MusicPlayer.html";
            Console.WriteLine("current directory is: " + uri);

            if (uri.Contains("spotify"))
            { 
                Process process = new Process();
                process.StartInfo.FileName = "cmd";
                process.StartInfo.Arguments = "/C START " + uri;
                process.Start();
            }
            else
            {
                Console.WriteLine("no spotify url found, launching other service");
            }

            


            
            
            Process process2 = new Process();
            process2.StartInfo.FileName = chromepad + "chrome.exe";
            process2.StartInfo.Arguments = "--enable-logging --v=1 " + website;
            process2.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            process2.Start();

            string path = @"pcs.txt";
            System.IO.File.WriteAllText(path, "playing");


            
           
        }
		//to keep track of the total time the song has been playing, it will count while the songs is playing, its not completely accurate, but this was the only way to check on that song time data and act upon it
        private static void MusicPlayer()
        {
            string path = @"pcs.txt";
            int x = 1;
            int y = 0;
            string songtoplay2 = songtoplay();
            string[] songplay = songtoplay2.Split('|');
            int timer = 0;
            
            try
            {
                if (String.IsNullOrEmpty(songplay[1]))
                {
                    Console.WriteLine("no timer");
                    Console.WriteLine("no song found, waiting for next check");
                    System.Threading.Thread.Sleep(500);
                    MusicPlayer();

                }
                else
                {
                    string timerms = songplay[1];
                    timer = Convert.ToInt32(timerms);
                   

                    MusicLauncher(songplay[2], timer);
                    Console.WriteLine("song uri: " + songplay[2]);

                    while (x != 0)
                    {




                        var musicstatus = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);// opens temp storage file
                        var musicstatusr = new StreamReader(musicstatus, Encoding.Default); //maket it able to read temp storage file

                        string musicstatusout = musicstatusr.ReadToEnd();// reads again the hole thing

                        if (musicstatusout == "playing")
                        {
                            y++;
                            Console.WriteLine("song is playing, counter: " + y);
                        }
                        else
                        {
                            Console.WriteLine("song is paused, counter: " + y);
                            x++;
                        }

                        if (y == timer)
                        {
                            string htmlpage = @"MusicPlayer.html";
                            File.Delete(htmlpage);
                            songtodelete(songplay[2]);
                            MusicPlayer();
                            break;
                        }


                        System.Threading.Thread.Sleep(1000);

                    }
                }

            }
            catch
            {
                Console.WriteLine("something wrong, but trys it again");
                MusicPlayer();
            }

                
                // System.Threading.Thread.Sleep(2000);
                
            
        }
		//checks which song has been the most requested i think, and then launches that song, if they are all once requested it will just play the first thing requested. First comes, first gets.
        private static string songtoplay()
        {
            string file = @"reqsongs.xml";
            string response;
            try
            {
                var xmlfileopen = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);// opens temp storage file
                var xmlfileread = new StreamReader(xmlfileopen, Encoding.Default); //maket it able to read temp storage file     
                string xmldata = xmlfileread.ReadToEnd();
                

                if (xmldata.Contains("<song>"))
                {

                    XmlDocument xml = new XmlDocument();
                    xml.Load(file);

                    XDocument xDoc = XDocument.Load(file);
                    int maxNr = xDoc.Root.Elements().Max(x => (int)x.Element("Times_Requested"));


                    string requested = xml.SelectSingleNode("Table/song[Times_Requested='" + maxNr + "']/Song_Name").InnerText;
                    string songtime = xml.SelectSingleNode("Table/song[Times_Requested='" + maxNr + "']/Song_Time").InnerText;
                    string uri = xml.SelectSingleNode("Table/song[Times_Requested='" + maxNr + "']/uri").InnerText;
                    response = requested + "|" + songtime + "|" + uri;
                    return response;
                }
                else
                {
                    response = "null |";
                    return response;
                }
            }
            catch
            {
                createxml();
                response = "no file";
                return response;
            }
            

           
        }
		//after the song is finishd, it will be deleted from the xml file, so it wont interfere with the next song that will be played
        private static void songtodelete(string uri)
        {



            string file = @"reqsongs.xml";
            XmlDocument xml = new XmlDocument();
            xml.Load(file);

            foreach (XmlNode node in xml.SelectNodes("Table/song"))
            {
                if (node.SelectSingleNode("uri").InnerText == uri)
                {
                    node.ParentNode.RemoveChild(node);
                }

            }

            xml.Save(file);
            
            
           
            
        }


		//this actually creates the xml file if it does not exist yet
        private static void createxml()
        {
            
            XmlTextWriter creator = new XmlTextWriter("reqsongs.xml", System.Text.Encoding.UTF8);
            creator.WriteStartDocument(true);
            creator.Formatting = Formatting.Indented;
            creator.Indentation = '2';
            creator.WriteStartElement("Table");
            //createNode(SongURI, SongName, SongArtist, SongTime, Req, creator);
            creator.WriteEndElement();
            creator.WriteEndDocument();
            creator.Close();

            XmlTextWriter creator2 = new XmlTextWriter("status.xml", System.Text.Encoding.UTF8);
            creator2.WriteStartDocument(true);
            creator2.Formatting = Formatting.Indented;
            creator2.Indentation = '2';
            creator2.WriteStartElement("status");
            creator2.WriteString("0");
            //createNode(SongURI, SongName, SongArtist, SongTime, Req, creator);
            creator2.WriteEndElement();
            creator2.WriteEndDocument();
            creator2.Close();
            Console.Write("xml2 created");
        }

        private static void createNode(string uri, string Sname, string Aname, string Stime, string req, XmlTextWriter creator)
        {
                creator.WriteStartElement("Song");
                creator.WriteStartElement("uri");
                creator.WriteString(uri);
                creator.WriteEndElement();
                creator.WriteStartElement("Song_Name");
                creator.WriteString(Sname);
                creator.WriteEndElement();
                creator.WriteStartElement("Artist_Name");
                creator.WriteString(Aname);
                creator.WriteEndElement();
                creator.WriteStartElement("Song_Time");
                creator.WriteString(Stime);
                creator.WriteEndElement();
                creator.WriteStartElement("Times_Requested");
                creator.WriteString(req);
                creator.WriteEndElement();
                creator.WriteEndElement();
        }

		//to get the uri from a spotify song, it has to scan the html source of the player where the song would play, to get its data <- thank you spotify for leaving this unchanged  :)
        private static string[] addsong(string requri)
        {
            string tempsongfile = @"tempfilesong.txt";
            System.IO.File.WriteAllText(tempsongfile, requri);//create and test the temp storage file

            var tempsongfileopen = new FileStream(tempsongfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);// opens temp storage file
            var tempsongfileread = new StreamReader(tempsongfileopen, Encoding.Default); //maket it able to read temp storage file

            string uri = tempsongfileread.ReadToEnd();//stores data from tempsongfile in uri
            string link = "https://embed.spotify.com/?uri=" + uri;
            string songnamesearch = "<div class=\"title-content ellipsis\">";
            string artistsearch = "<div class=\"artist-name ellipsis\" rel=\"";
            string durationsearch = "data-duration-ms=\"";

            WebClient client2 = new WebClient();//starts new webclient on var client
            client2.Headers.Add("user-agent", "Mozilla/5.0 (Windows; U; Windows NT 5.1; en-US; rv:1.8.1.13) Gecko/20080311 Firefox/2.0.0.13");
            string spotidata = client2.DownloadString(link);//download data from given url

            int possongname = spotidata.IndexOf(songnamesearch);
            string songnameunref = spotidata.Substring(possongname, 150);
            int possongname_end = songnameunref.IndexOf("</div>");
            string songnameunref2 = spotidata.Substring(possongname, possongname_end);
            string songname = songnameunref2.Replace(songnamesearch, "");

            int posartist = spotidata.IndexOf(artistsearch);
            string artistunref = spotidata.Substring(posartist, 150);
            int posartist_end = artistunref.IndexOf("\"></div>");
            string artistunref2 = spotidata.Substring(posartist, posartist_end);
            string artist = artistunref2.Replace(artistsearch, "");

            int posduration = spotidata.IndexOf(durationsearch);
            string durationunref = spotidata.Substring(posduration, 150);
            int posduration_end = durationunref.IndexOf("\" data-index=\"");
            string durationunref2 = spotidata.Substring(posduration, posduration_end);
            string durationms = durationunref2.Replace(durationsearch, "");
            int durationsec = Convert.ToInt32(durationms) / 1000;
            string duration = Convert.ToString(durationsec);

            int poscommasongname = songname.IndexOf("&#039;");
            int poscommaartist = artist.IndexOf("&#039;");
            string songnameesc;
            string artistesc;

            if (poscommasongname > 1)
            {
                songnameesc = songname.Replace("&#039;", "\'");
            }
            else
            {
                songnameesc = songname;
            }

            if (poscommaartist > 1)
            {
                artistesc = artist.Replace("&#039;", "\'");
            }
            else
            {
                artistesc = artist;
            }
            string adddata = addxml(songnameesc, artistesc, duration, uri, "1");

            

           

            string[] songinfo = new string[] { artistesc, songnameesc, duration, adddata };

            return songinfo;
     
        }

		//this checks chrome debug file, to check if you pressed the pause button on the webplayer, since that invokes an action that can be recorded in chromes debug file, so i just have to scan for it :)
		//this is highly in efficient though, just a small change to the way chrome debugs and this program can be trowen in the trash can :S
        private static void checkdeb()
        {
            //variables to check songs
            string play = "playing.html";
            string pause = "paused.html";
            string line;
            string musicstatusout;
            string play2;
            string pause2;
            string test = "null";
            string path = @"pcs.txt";
            System.IO.File.WriteAllText(path, test);//create and test the temp storage file
           

            int x = 1;// for infinite while loop
            while (x != 0)
            {
                var file_open = new FileStream(@"C:\Users\Eldin\AppData\Local\Google\Chrome\User Data\chrome_debug.log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);//opens debug  file from chrome to read
                var reader = new StreamReader(file_open, Encoding.Default);//with this it can read the file
                if (reader.BaseStream.Length > 5048)//if file is bigger then 1 kb
                {
                    reader.BaseStream.Seek(-5048 , SeekOrigin.End);//start reading from the end, till 1024 bytes.
                }
               
                line = reader.ReadToEnd();// reads the hole thing at onece

                var musicstatus = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);// opens temp storage file
                var musicstatusr = new StreamReader(musicstatus, Encoding.Default); //maket it able to read temp storage file

                musicstatusout = musicstatusr.ReadToEnd();// reads again the hole thing

                

                int pospause = line.IndexOf(pause);// searches for the position of the first character using var pause to search in chrmdeb file.
                //Console.Write("pause position= " + pospause + "\r\n");//outputs position of first occurance

                int posplay = line.IndexOf(play);// searches for the position of the first character using var play to search in chrmdeb file.
                //Console.Write("play position= " + posplay + "\r\n");//outputs position of first occurance
                
                if (posplay > 1 )//if postition of play is bigger then 1
                {
                    play2 = "playing"; //set var play2 to playing
                    test = "true";//and test to true
                    System.IO.File.WriteAllText(path, play2);//write to temp file that its playing
                    
                }
                else//needed otherwise c# gets unsure about if the variable will be set or not
                {
                    play2 = "null";
                    test = "true";
                }
               
                if(pospause > 1)//if position of pause is bigger then 1
                {
                    pause2 = "paused";//set var pause2 to paused
                    test = "false";//and test to false
                    System.IO.File.WriteAllText(path, pause2);//write tot temp file that song is pauzed
                }
                else//needed otherwise c# gets unsure about if the variable will be set or not
                {
                    test = "null";
                    pause2 = "false";
                }
               //outputs everything 
              //Console.Write("play = " + play2 + "\r\n");
               // Console.Write("posplay = " + posplay + "\r\n");
               // Console.Write("status = " + test + "\r\n");
              // Console.Write("music = " + musicstatusout + "\r\n");
                
               
                System.Threading.Thread.Sleep(100);//pauses loop vor 1/10 of a second, to relieve stress from the cpu (fx-6100 4.1 ghz = 0.5 % usage)
            }
        }

		//this adds the song data to the xml file
        private static string addxml(string SongName, string SongArtist, string SongTime, string SongURI, string Req)
        {
            string response;
            string file = @"reqsongs.xml";
            string check = readxml(SongURI);
            if (check != "true")
            {
                
                XmlDocument doc = new XmlDocument();
                doc.Load(file);

                XmlNode node = doc.CreateNode(XmlNodeType.Element, "song", null);
                XmlNode nodeuri = doc.CreateElement("uri");
                nodeuri.InnerText = SongURI;
                XmlNode Song_Name_Node = doc.CreateElement("Song_Name");
                Song_Name_Node.InnerText = SongName;
                XmlNode Artist_Name_Node = doc.CreateElement("Artist_Name");
                Artist_Name_Node.InnerText = SongArtist;
                XmlNode Song_Time_Node = doc.CreateElement("Song_Time");
                Song_Time_Node.InnerText = SongTime;
                XmlNode Times_Requested_Node = doc.CreateElement("Times_Requested");
                Times_Requested_Node.InnerText = Req;

                node.AppendChild(nodeuri);
                node.AppendChild(Song_Name_Node);
                node.AppendChild(Artist_Name_Node);
                node.AppendChild(Song_Time_Node);
                node.AppendChild(Times_Requested_Node);

                doc.DocumentElement.AppendChild(node);

                doc.Save(file);

                Console.Write("xml added\r\n ");

                response = "added to requests";
                
            }
            else
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(file);
                string requested = xml.SelectSingleNode("Table/song[Song_Name='" + SongName + "']/Times_Requested").InnerText;
                int trequested = Convert.ToInt32(requested);
                int newtrequested = trequested + 1;
                string newrequested = Convert.ToString(newtrequested);
                xml.SelectSingleNode("Table/song[Song_Name='" + SongName + "']/Times_Requested").InnerText = newrequested;
                xml.Save(file);

                response = "updated, song has been " + newrequested + " times requested";
            }

            return response;

        }

		//this reads the xml file
        private static string readxml(string check)
        {

            string file = @"reqsongs.xml";
            var xmlfileopen = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);// opens temp storage file
            var xmlfileread = new StreamReader(xmlfileopen, Encoding.Default); //maket it able to read temp storage file     
            string xmldata;
            string xmlsearch = "<uri>";
            string xmlsearch2 = "</uri>";
            int searchlength = xmlsearch.Length;
            int searchlength2 = xmlsearch2.Length - 1;
            int x = 0;
            string xmlsearchout;
            //string check = "Temple Secrets";
            string response = "false";

            while ((xmldata = xmlfileread.ReadLine()) != null)
            {

                if (xmldata.Contains(xmlsearch))
                {
                    int pos1 = xmldata.IndexOf(xmlsearch);
                    int pos2 = xmldata.IndexOf(xmlsearch2);
                    xmlsearchout = xmldata.Substring(pos1 + searchlength, pos2 - pos1 - searchlength2);

                    if (check != xmlsearchout)
                    {
                       // Console.WriteLine(xmlsearchout + " <-xml,check-> " + check);
                        //check = xmlsearchout;
                        response = "false";
                      
                    }
                    else
                    {
                        //Console.WriteLine("already readed once");
                        response = "true";
                       
                    }
                    
                }
                else
                {
                    //Console.WriteLine("does not fit search input");
                    xmlsearchout = "nothing";
                    response = "false";
                   
                }

                if (response == "true")
                {
                    //Console.WriteLine("found match");
                    break;
                    
                }

                x++;

            }


            return response;
          
        }

		//this works only for me(rareamv right now, this option will be eventually available in the gui, but for now its not so important)
        public static string steamip()
        {
            
            string Url = "http://steamcommunity.com/id/rareamv";// var url contains the url to be parsed
            string Search = "steamid";// var search contains the string to search for
            

            WebClient client = new WebClient();//starts new webclient on var client
            string downloadString = client.DownloadString(Url);//download data from given url

            int Occurance = downloadString.IndexOf(Search);//searches for var Search in download string

            string SteamIDUnCut = downloadString.Substring(Occurance, 28);//cuts data at result of search, makes it 28 chars long
            string SteamIDUnCut2 = SteamIDUnCut.Replace("steamid\":\"", "");//cuts away unessary data
            string SteamID = SteamIDUnCut2.Replace("\"", "");//also cuts away unessary data, clean result is given

            //vars needed to get current ip from the server that the client is playing on(ONLY VALVE GAMES)
            string PlayerSummaries = "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=5B0D5560CC102258A606E76406267A46&steamids=" + SteamID;
            WebClient client3 = new WebClient();//start new webclient on var client3
            string PlayerData = client3.DownloadString(PlayerSummaries);//downloads data from PLayerSummaries
            string SearchRname = "\"gameserverip\":";
            string SearchEndRname = "\",";

            int SearchRnameOcc = PlayerData.IndexOf(SearchRname);//searches for location of first character, using SearchRname to search with.

            string RnameUnCut = PlayerData.Substring(SearchRnameOcc, 100);// cuts data from given location by search, makes it 100 chars long

            int EndRnameOcc = RnameUnCut.IndexOf(SearchEndRname);//to check length of ip, searches first occurance of SearchEndRname

            string RnameUnCut2 = PlayerData.Substring(SearchRnameOcc, EndRnameOcc);//cuts from SearchRnameOcc to EndRnameOcc
            string RnameUnCut3 = RnameUnCut2.Replace(SearchRname, "");// replaces SearchRname with nothing.
            string Rname = RnameUnCut3.Replace("\"", "");// replaces backslash with nothing

            return Rname;
        }
		//well, the actual backbone to this program, it connects to the irc server, and retrieves/sends data to it.
        private static void spotibot()
        {

            Console.WriteLine("Waiting for joining the irc server");
            string config = "configtw.ini"; // you can use configtw.ini for twitch and configju.ini for justin or just name it whatever you want
            var configopen = new FileStream(config, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);// opens temp storage file
            var configread = new StreamReader(configopen, Encoding.Default); //maket it able to read temp storage file     
            string configdata = configread.ReadToEnd();
            string[] configdataarr = configdata.Split('|');

            Console.WriteLine("usernametest : " + configdataarr[1]);

            //vars for connecting to server, will be controlled by gui lateron
            string Server = configdataarr[5];
            int Port = 6667;
            string User = configdataarr[1];
            string Auth = configdataarr[3];
            string Channel = "#" + configdataarr[1];

            TcpClient Client = new TcpClient(Server, Port);//The tcp connect command, and asssigns to var so it can be used to get its stream contents
            NetworkStream NwStream = Client.GetStream();//website says get TCP/Network Stream and assign it to NwStream, i guess this is where all the data from the server will pass or something.
            StreamReader Reader = new StreamReader(NwStream); // I think i was right, this reads what is passed to NwStream into the Reader var, so reads the data from irc server
            StreamWriter Writer = new StreamWriter(NwStream); // this assasigns the writing ability to the var writer, to be used as Writer.WriteLine("your command to the server, in this case, irc commands")
           
            //logging in on the irc server will be done here:
            Writer.WriteLine("PASS " + Auth); // sends the authentication password to irc server, this is for twitch
            Writer.Flush(); // according to the tutorial, each time after passings something to the server, it needs to be flushed/reset?
            Writer.WriteLine("NICK " + User); // tells the irc server that its this "User" that joins
            Writer.Flush();
            Writer.WriteLine("JOIN " + Channel);// Joins the channel on the current irc server
            Writer.Flush();

            string Data = ""; // according to tut: to receive data, meaning that data from Reader will prob stored here
            while ((Data = Reader.ReadLine()) != null)// when there is data to read it will do the loop thing
            {
                Console.WriteLine(Data); // writes data to console, seems im right btw about the data string
                if (Data.Contains("!test"))//if someone types !test, the command stuff under me will execute:
                {
                    string[] words = Data.Split(' ');//splits current string into words that can be accessed trough arrays.
                    Writer.WriteLine("PRIVMSG #rareamv : wassup nigga, first c# bot made by rareamv here");//like i said above, this will be executed when !test is typed
                    Writer.Flush();
                    int counter = words.Count();// counts all elements in array

                    if (counter == 5)//if counter equals 5,(then there is something behind command)
                    {
                        Writer.WriteLine("PRIVMSG #rareamv : what ya typed behind command: " + words[4]);//put out this line + input behind command
                        Writer.Flush();
                    }
                    else
                    {
                        Writer.WriteLine("PRIVMSG #rareamv : test did not contain words " + counter);//says total of elements in array words
                        Writer.Flush();
                    }
                    Console.WriteLine(" test executes ");
                }
                else if (Data.Contains("!ip"))
                {

                    string Rname = steamip();
                    Writer.WriteLine("PRIVMSG #rareamv : Current Ip of server: " + Rname);//outputs ip
                    Writer.Flush();//flushses what it just wrote to server
                    Console.WriteLine("Server IP: " + Rname);// put it in console for debug poss
                }
                else if(Data.Contains("!request"))
                {
                    string[] words = Data.Split(' ');//splits current string into words that can be accessed trough arrays.
                    int counter = words.Count();// counts all elements in array

                    if (counter == 5)//if counter equals 5,(then there is something behind command)
                    {
                       // System.IO.File.WriteAllText(tempfilesong, words[4]);//write uri to temp file
                        string[] songinfo = saveSongData(words[4]);

                        Writer.WriteLine("PRIVMSG #rareamv : Songinfo : " + songinfo[0] + " - " + songinfo[1]);//says total of elements in array words
                        Writer.Flush();
                        Writer.WriteLine("PRIVMSG #rareamv : Request is : " + songinfo[3]);//says total of elements in array words
                        Writer.Flush();
                    }
                    else
                    {
                        Writer.WriteLine("PRIVMSG #rareamv : wrong command, usage: !request [spotiuri]");//says total of elements in array words
                        Writer.Flush();
                    }
                }
                else
                {
                    //well, if the command is wrong i guess this will be said
                    Writer.Flush();
                    Console.WriteLine(" else executes ");
                }


            }
        }

    }


}
