using Microsoft.AspNetCore.SignalR;

namespace OrderManagement.Hubs
{
    public class OrderHub : Hub
    {
        // Siparişin durumunu güncelleme bildirimi
        public async Task SendOrderStatusUpdate(int orderId, string status, bool isProcessing)
        {
            await Clients.All.SendAsync("ReceiveOrderStatusUpdate", orderId, status, isProcessing);
        }
        public async Task JoinCustomerGroup(string customerId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, customerId);
        }
        public async Task LeaveCustomerGroup(string customerId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, customerId);
        }
    }
}
