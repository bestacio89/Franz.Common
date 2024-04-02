using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IMessageDeserializer
{
  object Deserialize(string message); // Replace "object" with your actual return type if known
}