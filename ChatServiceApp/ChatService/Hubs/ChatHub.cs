using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatService.Hubs
{
    public class ChatHub : Hub
    {
        private readonly string _botUser;
        private readonly IDictionary<string, UserConnection> _connections; // inject instance to ChatHub

        //constructor
        public ChatHub(IDictionary<string, UserConnection> connections)
        {
            _botUser = "MyChat Bot";
            _connections = connections;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {   //if there is a connection id, gets value of the userConnection
            if (_connections.TryGetValue(Context.ConnectionId, out UserConnection userConnection))
            {
                //remove the connection id/user in the dictionary
                _connections.Remove(Context.ConnectionId);
                Clients.Group(userConnection.Room).SendAsync("ReceiveMessage", _botUser, $"{userConnection.User} has left");
                SendUsersConnected(userConnection.Room);
            }

            return base.OnDisconnectedAsync(exception);
        }

        // method for sending messages
        public async Task SendMessage(string message)
        {
            //if there is a connection id, gets value of userConnection
            if(_connections.TryGetValue(Context.ConnectionId, out UserConnection userConnection))
            {
                //sends message into room by the user
                await Clients.Group(userConnection.Room)
                        .SendAsync("ReceiveMessage", userConnection.User, message);
            }
        }
        public async Task JoinRoom(UserConnection userConnection)
        {
            //group incoming connection with Room connection, communicate with those inside the current room
            await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.Room);
            //keeps track of users joining by id
            _connections[Context.ConnectionId] = userConnection;
            //send message to clients inside the Room group
            await Clients.Group(userConnection.Room).SendAsync("ReceiveMessage", _botUser,
                $"{userConnection.User} has joined {userConnection.Room}");
            await SendUsersConnected(userConnection.Room);
        }
        

        //shows chat room whenever users leave or enter a chatroom 
        public Task SendUsersConnected(string room)
        {
            var users = _connections.Values
                .Where(c => c.Room == room)
                .Select(c => c.User);

            return Clients.Group(room).SendAsync("UsersInRoom", users);
        }

    }
}
