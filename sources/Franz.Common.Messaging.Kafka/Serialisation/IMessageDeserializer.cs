using Franz.Common.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public interface IMessageDeserializer<TMessage> where TMessage : Message
{
  TMessage Deserialize(string message); // Specify the return type as TMessage
}
