using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace Streaming
{
    /// Herhangi bir görüntü kaynağından istemciye görüntü aktarılabilmesi için sunucu oluşturur.
    public class ImageStreamingServer : IDisposable
    {
        private List<Socket> _Clients; //client listesini oluşturuyoruz
        private Thread _Thread; //thread oluşturuyoruz
        //string connected = ""; 
        public ImageStreamingServer() : this(ScreenShare.Snapshots(0, 0)) { }
        public ImageStreamingServer(IEnumerable<Image> imagesSource)
        {
            _Clients = new List<Socket>();
            _Thread = null;

            this.ImagesSource = imagesSource;
        }

        /// Bağlı olan herhangi bir istemciye aktarılacak görüntü kaynağını alır veya ayarlar.
        public IEnumerable<Image> ImagesSource { get; set; }

        /// Sunucunun durumunu döndürür. Gerçek şu ki, sunucu şu anda çalışıyor ve herhangi bir istemci isteğine hizmet vermeye hazır.
        public bool IsRunning { get { return (_Thread != null && _Thread.IsAlive); } }

        /// Belirtilen bağlantı noktasında yeni bağlantıları kabul edecek şekilde sunucuyu başlatır.
        public void Start(int port)
        {
            lock (this)
            {
                _Thread = new Thread(new ParameterizedThreadStart(ServerThread));
                _Thread.IsBackground = true;
                _Thread.Start(port);
            }
        }

        /// Bu, istemcilerin tüm yeni bağlantılarına hizmet veren sunucunun ana iş parçacığı.
        private void ServerThread(object state)
        {
            try
            {
                Socket Server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                Server.Bind(new IPEndPoint(IPAddress.Any, (int)state));
                Server.Listen(10);
                // System.Diagnostics.Debug.WriteLine(string.Format("Server started on port {0}.", state)); //state 8080
                foreach (Socket client in Server.IncommingConnectoins())
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ClientThread), client);

            }
            catch { }
        }

        /// Her istemci bağlantısı bu iş parçacığı tarafından sunulacak.
        public void ClientThread(object client)
        {
            Socket socket = (Socket)client;
            //connected = socket.RemoteEndPoint.ToString();
            //System.Diagnostics.Debug.WriteLine(string.Format("New client from {0}", connected));
            lock (_Clients) _Clients.Add(socket);

            try
            {
                using (MjpegWriter wr = new MjpegWriter(new NetworkStream(socket, true)))
                {
                    // Yanıt üstbilgisini istemciye yazar.
                    wr.WriteHeader();

                    // Kaynaktaki görüntüleri istemciye aktarır.
                    foreach (var imgStream in ScreenShare.Streams(this.ImagesSource))
                    {
                        wr.Write(imgStream);
                    }
                }
            }
            catch { }
            finally
            {
                lock (_Clients)
                    _Clients.Remove(socket);
            }
        }

        public void Dispose()
        {
        }
    }

    static class SocketExtensions
    {

        public static IEnumerable<Socket> IncommingConnectoins(this Socket server)
        {
            while (true)
                yield return server.Accept();
        }

    }

    static class ScreenShare
    {
        public static IEnumerable<Image> Snapshots(int width, int height)
        {
            Size size = new Size(1920, 1080); //gönderdiğimiz ekran boyutu System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width

            Bitmap srcImage = new Bitmap(1920, 1080); //gönderdiğimiz image boyutu
            Graphics srcGraphics = Graphics.FromImage(srcImage);

            Bitmap dstImage = srcImage;
            Graphics dstGraphics = srcGraphics;

            while (true)
            {
                srcGraphics.CopyFromScreen(0, 0, 0, 0, size);
                yield return dstImage;
            }
        }

        internal static IEnumerable<MemoryStream> Streams(this IEnumerable<Image> source)
        {
            MemoryStream ms = new MemoryStream();

            foreach (var img in source)
            {
                ms.SetLength(0);//0
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                yield return ms;
            }

            ms.Close();
            ms = null;
            yield break;
        }
    }
}
