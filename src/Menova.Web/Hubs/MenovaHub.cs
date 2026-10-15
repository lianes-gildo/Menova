using Microsoft.AspNetCore.SignalR;

namespace Menova.Web.Hubs;

public class MenovaHub : Hub
{
    public async Task JuntarSala(string nomeSala)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, nomeSala);
    }

    public async Task SairSala(string nomeSala)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, nomeSala);
    }
}
