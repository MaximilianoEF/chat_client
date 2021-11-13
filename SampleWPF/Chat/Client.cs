using System;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Serialization;
using Chat;

namespace ChatUI.Chat
{
    public class Client
    {
        //Events
        public delegate void UpdateDelegate(object o);
        public UpdateDelegate onUpdate;

        //Chat manager
        User u;
        ChatManager chat;

        //Chat connection
        readonly IPHostEntry host;
        readonly IPAddress addr;
        readonly IPEndPoint endPoint ;
        readonly Socket socket;
        Thread listenThread;

        public Client(string username, UpdateDelegate onUpdate)
        {
            this.onUpdate = onUpdate;

            this.u = new User(Guid.NewGuid().ToString("N"), username);
            this.chat = new ChatManager(u);

            try
            {
                host = Dns.GetHostEntry("localhost");
                addr = host.AddressList[0];
                endPoint = new IPEndPoint(addr, 4404);
                socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            } catch(Exception e)
            {
                
            }
            
        }

        /// <summary>
        /// Start the client
        /// </summary>
        public void Start()
        {
            this.socket.Connect(endPoint);
            this.Send(this.socket, this.u);
            listenThread = new Thread(this.Listen);
            listenThread.SetApartmentState(ApartmentState.STA);
            listenThread.Start();
        }

        /// <summary>
        /// Listen for messages and users from the server
        /// </summary>
        private void Listen()
        {
            object received;

            while(true)
            {
                received = this.Receive(this.socket);
                if(received is Message)
                {
                    this.AddMessage((Message)received);

                }
                else if(received is User)
                {
                    this.AddUser((User)received);
                }
            }
        }

        /// <summary>
        /// Add a user to the chat db
        /// </summary>
        /// <param name="user"></param>
        private void AddUser(User user)
        {
            if(user.id != this.u.id && this.chat.AddUser(user))
                onUpdate(user);   
        }

        /// <summary>
        /// Add a message to the chat db
        /// </summary>
        /// <param name="m">Message to add</param>
        private void AddMessage(Message m)
        {
            this.chat.AddMessage(m);
            onUpdate(m);
        }

        /// <summary>
        /// Get all messages received and sended to user
        /// </summary>
        /// <param name="u">User context</param>
        /// <returns></returns>
        public LinkedList<Message> GetMessages(User u)
        {
            return chat.GetMessages(u);
        }

        /// <summary>
        /// Get all the users
        /// </summary>
        /// <returns>Users</returns>
        public LinkedList<User> GetUsers()
        {
            return chat.GetUsers();
        }

        /// <summary>
        /// Send a message to the server
        /// </summary>
        /// <param name="msg"></param>
        public Message SendMessage(string msg, User to)
        {
            Message m = new Message(this.u, to, msg);
            this.Send(this.socket, m);
            this.AddMessage(m);
            return m;
        }

        /// <summary>
        /// Send a object to the client
        /// </summary>
        /// <param name="s">Socket client</param>
        /// <param name="o">Object to send</param>
        private void Send(Socket s, object o)
        {
            byte[] buffer = new byte[1024];
            byte[] obj = BinarySerialization.Serializate(o);
            Array.Copy(obj, buffer, obj.Length);
            s.Send(buffer);
        }

        /// <summary>
        /// Receive all the serialized object
        /// </summary>
        /// <param name="s">Socket that receive the object</param>
        /// <returns>Object received from client</returns>
        private object Receive(Socket s)
        {
            byte[] buffer = new byte[1024];
            s.Receive(buffer);
            return BinarySerialization.Deserializate(buffer);
        }
    }
}
