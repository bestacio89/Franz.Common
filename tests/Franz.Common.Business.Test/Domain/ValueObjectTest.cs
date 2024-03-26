using Franz.Common.Business.Domain;
using Xunit;

namespace Franz.Common.Business.Tests.Domain;

public class ValueObjectTest
{
  private static readonly ValueObject FicheEtatCivileUnique = CreateDefaultFicheEtatCivileValueObject();
  private static readonly ValueObject AddresseUnique = CreateDefaulAddresse();

  [Theory]
  [MemberData(nameof(EqualValueObjects))]
  public void Equals_SameObjects_ReturnsTrue(ValueObject? instanceA, ValueObject? instanceB, string raison)
  {
    var result = EqualityComparer<ValueObject>.Default.Equals(instanceA, instanceB);

    Assert.True(result, raison);
  }

  [Theory]
  [MemberData(nameof(NoEqualValueObjects))]
  public void Equals_NoSameObjects_ReturnsFalse(ValueObject? instanceA, ValueObject? instanceB, string raison)
  {
    var result = EqualityComparer<ValueObject>.Default.Equals(instanceA, instanceB);

    Assert.False(result, raison);
  }

  [Fact]
  public void Equals_NoSameTypeOfValueObject_ReturnsFalse()
  {

    var result = FicheEtatCivileUnique.Equals(AddresseUnique);

    Assert.False(result, "Les deux ValuesObject ne sont pas de même type.");
  }

  [Fact]
  public void Copy_SameValueObjectAfter_ReturnsTrue()
  {
    var AddresseCopie = AddresseUnique.GetCopy();

    Assert.True(AddresseCopie.Equals(AddresseUnique));
  }

  private static FicheEtatCivile CreateDefaultFicheEtatCivileValueObject()
  {
    return new("John", "Doe", 32);
  }

  private static Addresse CreateDefaulAddresse()
  {
    return new("Ligne 1", "Ligne 2", "Strasbourg", "67300");
  }

  public class FicheEtatCivile : ValueObject
  {
    public string Nom { get; init; }
    public string Prenom { get; init; }
    public int Age { get; init; }

    public FicheEtatCivile(string nom, string prenom, int age)
    {
      Nom = nom;
      Prenom = prenom;
      Age = age;
    }


    protected override IEnumerable<object> GetEqualityComponents()
    {
      yield return Nom;
      yield return Prenom;
      yield return Age;
    }
  }

  public class Addresse : ValueObject
  {
    public Addresse(string ligne1, string ligne2, string ville, string codepostal)
    {
      Ligne1 = ligne1;
      Ligne2 = ligne2;
      Ville = ville;
      Codepostal = codepostal;
    }

    public string Ligne1 { get; init; }
    public string Ligne2 { get; init; }
    public string Ville { get; init; }
    public string Codepostal { get; init; }

    protected override IEnumerable<object> GetEqualityComponents()
    {
      yield return Ligne1;
      yield return Ligne2;
      yield return Ville;
      yield return Codepostal;
    }
  }

  public static readonly TheoryData<ValueObject?, ValueObject?, string> EqualValueObjects = new()
  {
    {
      null,
      null,
      "Doivent être égales car les deux instances sont nulles."
    },
    {
      FicheEtatCivileUnique,
      FicheEtatCivileUnique,
      "Doivent être égales car c'est la même instance ValueObject."
    },
    {
      new FicheEtatCivile("Albert", "Einstein", 70),
      new FicheEtatCivile("Albert", "Einstein", 70),
      "Doivent être égales car ces deux ValueObject ont les mêmes égalités de membres."
    }
  };

  public static readonly TheoryData<ValueObject, ValueObject, string> NoEqualValueObjects = new()
  {
    {
      new FicheEtatCivile("Albert", "Einstein", 40),
      new FicheEtatCivile("Albert", "Einstein", 70),
      "Ne doivent pas être égales car ils différent sur l'âge."
    },
          {
      new FicheEtatCivile("Albert", "Einstein", 70),
      new FicheEtatCivile("Albert", "Enstein", 70),
      "Ne doivent pas être égales car ils différent sur le nom."
    },
    {
      new FicheEtatCivile("Albert", "Einstein", 70),
      new FicheEtatCivile("Alberts", "Einstein", 70),
      "Ne doivent pas être égales car ils différent sur le prénom."
    }
  };
}
