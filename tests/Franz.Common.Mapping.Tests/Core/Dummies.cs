using Franz.Common.Mapping.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Franz.Common.Mapping.Tests.Core;

public class User
{
  public string Name { get; set; }
  public Address Address { get; set; }
}

public class UserDto
{
  public string Name { get; set; }
  public AddressDto Address { get; set; }
}

public class Product
{
  public decimal Price { get; set; }
}

public class ProductDto
{
  public decimal Price { get; set; }
}

public class Address
{
  public string City { get; set; }
}

public class AddressDto
{
  public string City { get; set; }
}

public class Node
{
  public Node Child { get; set; }
}

public class NodeDto
{
  public NodeDto Child { get; set; }
}

[ValueObject]
public class WrappedInt
{
  public int Value { get; set; }
}