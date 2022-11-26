using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets
{
	public interface IChatClientListener
	{
		void onConnect();
		void onDisconnect();
		void onMessageByUser(string userId, string text);
		void onMessageByChat(string chatName, string userId, string text);
	}
}
