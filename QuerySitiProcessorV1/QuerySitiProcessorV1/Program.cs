using HtmlAgilityPack;
using System;
using System.IO.Pipes;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace querySitiProcessorV1
{
    class Program
    {
        static DateTime _inizio;//Data d'inizio del processo, servirà a nominare il salvataggio e a fornire il tempo di vita del processo una volta terminato
        static readonly string _path = "C:\\Users\\f.fresi\\Documents\\_stage\\QuerySitiProcessorV1\\QuerySitiProcessorV1\\reps";//Percorso che porta alla cartella dove si trovano i salvataggi
        static readonly string _fNameBase = "rep";//Nome del file di base senza la data identificatica
        static readonly string _currentFile = "QueryTest.txt";//File dal quale ottengo i siti web
        static readonly string _keyWord = "covermanager";//Parola chiave
        static int _pageLength = 5;
        static int _startPage = 1;
        static void Main(string[] args)
        {
            Console.WindowWidth = 150;
            Console.WindowHeight = 30;
            Console.BufferHeight = 10000;

            do//Ciclo che si ripete fino a che l'utente non preme Esc e chiude il programma
            {           
                //FACCIO SCEGLIERE ALL'UTENTE SE APRIRE UN VECCHIO SALVATAGGIO O INIZARE UN NUOVO PROCESSO
                Console.Clear();
                writeLineColor("Websites Query Processor V1", ConsoleColor.Magenta);
                writeColor("Start Processing ", ConsoleColor.Green);//Enter-> Nuovo processo
                Console.WriteLine("-> Enter");
                writeColor("Check Previous Results: ", ConsoleColor.DarkCyan);//Space-> Apro vecchio processo
                Console.WriteLine("-> C");
                writeColor("System: ", ConsoleColor.Yellow);
                Console.WriteLine("-> S");
                writeColor("Exit: ", ConsoleColor.Red);//Exit-> Arresto il programma
                Console.WriteLine("-> Esc");
                bool success = false;
                ConsoleKeyInfo k;
                do//Ricezione dell'input dell'utente
                {
                    k = Console.ReadKey(true);
                    switch (k.Key)//Evoco una funzione diversa in base all'azione scelta dall'utente
                    {
                        case ConsoleKey.Enter:
                            _inizio = DateTime.Now;
                            startIter();
                            success = true;
                            break;
                        case ConsoleKey.C:
                            checkRep();
                            success = true;
                            break;
                        case ConsoleKey.S:
                            showSystem();
                            success = true;
                            break;
                        case ConsoleKey.Escape:
                            Console.Clear();
                            writeLineColor("Bye", ConsoleColor.Red);
                            Environment.Exit(0);
                            break;
                    }
                } while (!success);
                Console.ReadKey();
            } while(true);
        }
        //PROCESSO DI CONTATTO SITI WEB E CONTROLLO LA LORO ATTIVITA E CERCO UNA PAROLO CHIAVE, DOPO DI CHE STAMPO A VIDEO I RISULTATI
        static void startIter()
        {
            altM().Wait();//Chiamo un metodo asincrono per eseguire tutte le azioni collegate alla comunicazione su internet che sono asincrone e quindi non posso includerle in un metodo sincrono.
            DateTime fine = DateTime.Now;//Una volta finito 'altM' è finito il processamento dei siti web, quindi segno la data di conclusione processo
            var tp = fine - _inizio;//Calcolo il tempo di vita del processo
            string path = $"{_path}\\{_fNameBase}{_inizio:dd-MM-yyyy_HH.mm}.txt";//Ottengo il nome del file appena creato
            string save = File.ReadAllText(path);//Ottengo il testo del file appena salvato
            string[] c = save.Split('\r');
            updateResults(save, out int success, out int matchL1, out int matchL2, out int fail);//Processo il salvataggio dei dati e poi li stampo sulla console
            Console.WriteLine();
            writeColor("Total Time: ", ConsoleColor.DarkMagenta);
            Console.WriteLine(tp);
            writeColor("Websites Checked: ", ConsoleColor.DarkCyan);
            Console.WriteLine(c.Count() - 1);
            writeColor("Matched at Lev.1 Pings: ", ConsoleColor.Green);
            Console.WriteLine(matchL1);
            writeColor("Matched at Lev.2 Pings: ", ConsoleColor.Yellow);
            Console.WriteLine(matchL2);
            writeColor("No Match Pings: ", ConsoleColor.DarkYellow);
            Console.WriteLine(success);
            writeColor("Failed Pings: ", ConsoleColor.Red);
            Console.WriteLine(fail);
        }
        //APRO UN SALVATAGGIO E LO INTERPRETO PER STAMPARE I DATI 
        static void checkRep()
        {
            writeLineColor("\nPrevious Processes: ", ConsoleColor.Cyan);
            string[] f_names = Directory.GetFiles(_path);//Ottengo i path di tutti i file contenuti nella cartella indicata da _path
            int c = 1;
            foreach(string f in f_names)//Stampo sulla console i nomi di tutti i file ottenuti precedentemente
            {
                string[] pprts = f.Split('\\');
                Console.WriteLine(c + ") " + pprts[pprts.Length-1]);
                c++;
            }
            writeColor("Select a File: ", ConsoleColor.Cyan);//Faccio scegliere all'utente che file aprire
            bool success = false;//Indica se l'identificativo è stato inserito senza errori
            int sel = 0;//Variabile che conterrà il numero identificativo del file scelto da aprire dall'utente
            do//Ciclo che si ripete finché l'identificativo non viene inserito senza errori
            {
                try
                {
                    sel = Convert.ToInt32(Console.ReadLine());
                    //Ottengo il contenuto del file selezionato, lo elaboro e lo stampo
                    string text = File.ReadAllText(f_names[sel-1]);
                    string[] temp = text.Split('\r');
                    updateResults(text, out int ok, out int matchL1, out int matchL2, out int fail);
                    Console.WriteLine();
                    writeColor("Websites Checked: ", ConsoleColor.DarkCyan);
                    Console.WriteLine(temp.Count()-1);
                    writeColor("Matched at Lev.1 Pings: ", ConsoleColor.Green);
                    Console.WriteLine(matchL1);
                    writeColor("Matched at Lev.2 Pings: ", ConsoleColor.Yellow);
                    Console.WriteLine(matchL2);
                    writeColor("No Match Pings: ", ConsoleColor.DarkYellow);
                    Console.WriteLine(ok);
                    writeColor("Failed Pings: ", ConsoleColor.Red);
                    Console.WriteLine(fail);
                    success = true;
                    bool subscs = false;//Funziona nello stesso modo di success
                    do
                    {
                        //Chiedo all'utente se vuole vedere l'intera rielaborazione del processo selezionato
                        writeLineColor("Do You Wanto to See The Full Report?", ConsoleColor.Cyan);
                        writeColor("Y-> ", ConsoleColor.Cyan);//1-> Mostro l'intera rielaboriazione
                        Console.WriteLine("Yes");
                        writeColor("N-> ", ConsoleColor.Cyan);//o-> Non mostro l'intera rielaborazione e continuo con il rpogramma
                        Console.WriteLine("No");
                        ConsoleKeyInfo k = Console.ReadKey(true);
                        switch (k.Key)
                        {
                            case ConsoleKey.Escape:
                                subscs = true;
                                break;
                            case ConsoleKey.N:
                                subscs = true;
                                break;
                            case ConsoleKey.Y:
                                int conta = 1;
                                string[] lines = File.ReadAllLines(f_names[sel - 1]);
                                foreach (string line in lines)
                                {
                                    char val = line[line.Length - 1];
                                    string[] keyValue = line.Split('\t');// 0-> IDscheda | 1-> IndirizzoWEB
                                                                         //Stampo a video l'IDScheda e l'IndirizzoWEB
                                    Console.Write($"{conta++})");
                                    writeColor("KEY: ", ConsoleColor.DarkCyan);
                                    Console.Write($"{keyValue[0]} | ");
                                    writeColor("WebSite: ", ConsoleColor.Magenta);
                                    Console.Write($"{keyValue[1]} | ");
                                    switch (line[line.Length - 1])
                                    {
                                        case '1':
                                            writeColor($"{val}-> ", ConsoleColor.Green);
                                            Console.WriteLine("Match At Level 1");
                                            break;
                                        case '2':
                                            writeColor($"{val}-> ", ConsoleColor.Yellow);
                                            Console.WriteLine("Match At Level 2");
                                            break;
                                        case '3':
                                            writeColor($"{val}-> ", ConsoleColor.DarkYellow);
                                            Console.WriteLine("Successfull Ping, No Match");
                                            break;
                                        case '0':
                                            writeColor($"{val}-> ", ConsoleColor.Red);
                                            Console.WriteLine("Failed Ping");
                                            break;
                                    }
                                    subscs = true;
                                }
                                break;
                        }
                    } while (!subscs);
                }
                catch (FormatException ex)//Se non viene inserito un numero intero avviso l'utente e gli faccio inserire nuovamente il numero identificativo
                {
                    writeLineColor("ERROR: Must be an integer", ConsoleColor.Red);
                }
                if (sel - 1 < 0 || sel - 1 > f_names.Length)//Se viene inserito un indeficativo inesistente avviso l'utente e gli faccio inserire nuovamente il numero identificativo
                {
                    writeLineColor("ERROR: Selected file doesn't exists", ConsoleColor.Red);
                    success = false;
                }
                else
                    success = true;
            } while (!success);
        }
        //MOSTRO LA PAGINA CON LE IMPOSTAZIONI E OFFRO LA POSSIBILITA' DI PERSONALIZZARLE
        static void showSystem()
        {
            writeLineColor("\nSYSTEM:", ConsoleColor.DarkYellow);
            writeColor("Page Lenght: ", ConsoleColor.Yellow);
            Console.WriteLine(_pageLength);
            writeColor("Start Page: ", ConsoleColor.Yellow);
            Console.WriteLine(_startPage);
            writeLineColor("Do You Want To Modify?", ConsoleColor.DarkYellow);
            writeColor("Y-> ", ConsoleColor.Yellow);
            Console.WriteLine("Yes");
            writeColor("N-> ", ConsoleColor.Yellow);
            Console.WriteLine("No");
            bool success = false;
            ConsoleKeyInfo k;
            do
            {
                k = Console.ReadKey(true);
                switch(k.Key)
                {
                    case ConsoleKey.Escape:
                        success = true;
                        break;
                    case ConsoleKey.N: 
                        success = true; 
                        break;
                    case ConsoleKey.Y:
                        writeLineColor("\nMODIFY:", ConsoleColor.DarkYellow);
                        writeColor("Page Lenght: ", ConsoleColor.Yellow);
                        _pageLength = Convert.ToInt32(Console.ReadLine());
                        writeColor("Start Page: ", ConsoleColor.Yellow);
                        _startPage = Convert.ToInt32(Console.ReadLine());
                        success = true;
                        break;
                }
            } while (!success);
        }

        //CONTATTO SITI WEB E CONTROLLO LA LORO ATTIVITA E CERCO UNA PAROLO CHIAVE
        static async Task altM()
        {
            List<string> anchors;//Lista che conterrà i link presenti su una pagina web
            var client = new HttpClient();//Client per inviare le richieste ai siti web
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");//Imposto lo stesso User-Agent di Chrome
            string p = $"{_path}\\rep{_inizio:dd-MM-yyyy_HH.mm}.txt";//Ottengo l'indirizzo del file generato all'inizio del processo
            StreamWriter sw = new StreamWriter(p);//Streamwriter che modificherà il file generato all'inizio del processo
            string[] lines = File.ReadAllLines(_currentFile);//Ottengo tutte le righe del file contenente i siti web
            string outp = "";//Stringa che conterrà il testo da salvare sul file generato all'inizio del processo
            int c = 0;//c serve solo ad evitare che venga latta la prima riga del _currentFile dato che contiente la descrizione delle righe successive e non dei valori
            bool success;

            int startIndex = ((_startPage - 1) * _pageLength)+1;
            int count = _pageLength;
            var page = from line in lines.Skip(startIndex).Take(count).ToArray()
                       let result = string.Join("\n", line)
                       select result;

            foreach (string line in page)//foreach che si ripete tante volte quanto le righe di _currentFile
            {
                success = false;
                anchors = new List<string>();
                string[] keyValue = line.Split('\t');// 0-> IDscheda | 1-> IndirizzoWEB
                outp = $"{line}\t";
                //Stampo a video l'IDScheda e l'IndirizzoWEB
                Console.Write($"{c})");
                writeColor("KEY: ", ConsoleColor.DarkCyan);
                Console.Write($"{keyValue[0]} | ");
                writeColor("WebSite: ", ConsoleColor.Magenta);
                Console.WriteLine(keyValue[1]);
                string url = keyValue[1];//Copio l'IndirizzoWEB
                try
                {
                    HttpResponseMessage m = await client.GetAsync(url);//Contatto l'IndirizzoWEB 
                    var content = await m.Content.ReadAsStringAsync();//Salvo la risposta del server alla nostra richiesta
                    writeColor("Answer: ", ConsoleColor.Green);
                    //Stampo a video la risposta del sito web
                    Console.WriteLine(m);
                    if (content.Contains(_keyWord, StringComparison.InvariantCultureIgnoreCase))//Se trovo la corrispondenza della parola chiava cercata salvo il valore 2 sul file
                        outp += "1";//Match al livello di ricerca 1
                    else//Altrimenti visito i siti indicati negli anchor dell'html per cercare la parola chiave in quei siti
                    {
                        var htmlDoc = new HtmlDocument();
                        htmlDoc.LoadHtml(content);//Ottengo l'html della pagina iniziale

                        var a_nodes = htmlDoc.DocumentNode//Salvo in una variabile tutti gli anchors e i loro contenuti
                            .SelectNodes("//a");
                        foreach (var node in a_nodes)//Salvo nella lista i link contenuti negli anchors
                        {
                            if (node.Attributes["href"] != null)
                                anchors.Add(node.Attributes["href"].Value);
                            if (node.Attributes["src"] != null)
                                anchors.Add(node.Attributes["src"].Value);
                        }

                        var iframe_nodes = htmlDoc.DocumentNode
                                .SelectNodes("//iframe");
                        foreach (var node in iframe_nodes)
                        {
                            if (node.Attributes["href"] != null)
                                anchors.Add(node.Attributes["href"].Value);
                            if (node.Attributes["src"] != null)
                                anchors.Add(node.Attributes["src"].Value);
                        }

                        int index = 0;
                        while(index < anchors.Count)
                        {
                            if(anchors[index].Length < 3)
                                anchors.RemoveAt(index);
                            else if ( anchors[index].Substring(0, 6) == "mailto" || anchors[index][0] == '#' || anchors[index][0] == '/')
                                anchors.RemoveAt(index);
                            else
                                index++;
                        }
                        index = 0;
                        while (index < anchors.Count && !success)
                        {
                            url = anchors[index];
                            if (url.Substring(0, 4) != "http")//Se un indirizzo non comincia con http cerco di ricreare l'indirizzo originale
                            {
                                Uri bUri = new Uri(url);
                                Uri myUri;  myUri = new Uri(bUri, keyValue[1]);
                                url = myUri.AbsolutePath;
                            }
                            m = await client.GetAsync(url);//Faccio la richiesta ad un sito presente sulla pagina iniziale
                            content = await m.Content.ReadAsStringAsync();//Ottengo la risposta e salvo html
                            if (content.Contains(_keyWord, StringComparison.InvariantCultureIgnoreCase))//Controllo se l'html contiene la parola chiave che cerchiamo
                            {
                                outp += "2";//Match al livello di ricerca 2
                                success = true;
                            }
                            index++;
                        }
                        if (!success)
                            outp += "3";//Nessun match ma il sito risponde
                    }
                }
                catch (InvalidOperationException ex)//Se il sito web non risponde stampo il resoconto e salvo il valore 0 sul file
                {
                    writeColor($"ERROR: {ex}", ConsoleColor.Red);
                    outp += "0";//Il sito non risponde
                }
                sw.WriteLine(outp);//Salvo sul file il risultato ottenuto
                c++;
            }
            sw.Close();
        }
        //Metodi per ridurre la quantita di codice dedicata all'estetica dell'interfaccia
        static void writeColor(string outp, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(outp);
            Console.ForegroundColor = ConsoleColor.White;
        }
        static void writeLineColor(string outp, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(outp);
            Console.ForegroundColor = ConsoleColor.White;
        }
        //Elaboro il dati nel file di testo e restituisco i valori del processo desiderato
        static void updateResults(string text, out int success, out int matchL1, out int matchL2, out int fail)
        {
            success = 0;
            matchL1 = 0;
            matchL2 = 0;
            fail = 0;
            string[] c = text.Split('\r');
            for(int i = 0; i < c.Length-1; i++)
            {
                string s = c[i];
                if (s[s.Length - 1] == '3')
                    success++;
                else if (s[s.Length - 1] == '1')
                    matchL1++;
                else if (s[s.Length - 1] == '2')
                    matchL2++;
                else
                    fail++;
            }     
        }
    }
}