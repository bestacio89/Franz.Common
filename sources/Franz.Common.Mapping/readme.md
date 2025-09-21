Got it 👍 — let’s align the README with the correct versioning. Since this is **v1.5.1**, it slots right after your **v1.5.0 milestone** (Aras + event semantics).

Here’s the **finalized README** for `Franz.Common.Mapping`:

---

# **Franz.Common.Mapping**

*A lightweight, fast, and extensible object mapping library for the Franz ecosystem.*

**Current Version**: `1.5.2`

---

## 🌟 Overview

`Franz.Common.Mapping` is designed as an **AutoMapper++ alternative** — type-safe, DI-friendly, and integrated with the **Franz Framework**.
It provides:

* 🔹 **Simple by-name mapping** out of the box.
* 🔹 **Profiles** with `CreateMap`, `ForMember`, and `Ignore`.
* 🔹 **Configurable via DI** (`AddFranzMapping`).
* 🔹 **Expression-based mapping** (fast, cached, reflection-minimal).
* 🔹 **Extendable** with custom converters and profiles.

---

## 📦 Installation

```bash
dotnet add package Franz.Common.Mapping --version 1.5.1
```

---

## 🚀 Quick Start

### 1. Define a Profile

```csharp
using Franz.Common.Mapping.Core;
using Franz.Common.Mapping.Profiles;

public class BookProfile : FranzMapProfile
{
    public override void Configure(MappingConfiguration config)
    {
        var expr = CreateMap<Book, BookDto>()
            .ForMember(nameof(BookDto.Title), nameof(Book.Name))
            .ForMember(nameof(BookDto.ISBN), nameof(Book.Isbn))
            .Ignore(nameof(BookDto.SecretNotes));

        config.Register(expr);
    }
}

public class Book 
{ 
    public string Name { get; set; } = ""; 
    public string Isbn { get; set; } = ""; 
}

public class BookDto 
{ 
    public string Title { get; set; } = ""; 
    public string ISBN { get; set; } = ""; 
    public string SecretNotes { get; set; } = ""; 
}
```

---

### 2. Register in DI

```csharp
services.AddFranzMapping(cfg =>
{
    var profile = new BookProfile();
    profile.Configure(cfg);
});
```

---

### 3. Use the Mapper

```csharp
var mapper = provider.GetRequiredService<IFranzMapper>();

var book = new Book { Name = "The Hobbit", Isbn = "978-0261103344" };
var dto = mapper.Map<Book, BookDto>(book);

Console.WriteLine(dto.Title); // "The Hobbit"
Console.WriteLine(dto.ISBN);  // "978-0261103344"
```

---

## 🛠 Features

* ✅ **By-name mapping** fallback (zero config).
* ✅ **Profiles** for explicit control.
* ✅ **ForMember & Ignore** API.
* ✅ **Immutable, cached mapping expressions** for performance.
* ✅ **DI integration** with `AddFranzMapping`.
* ✅ **Tested in the Franz Book API** to ensure real-world readiness.

---

## 🧭 Roadmap

* 🔹 Custom type converters (`ITypeConverter<TSource, TDest>`).
* 🔹 Async mapping support for I/O heavy scenarios.
* 🔹 Startup validation (fail-fast if mappings are incomplete).
* 🔹 Roslyn analyzers to catch missing profiles at compile time.

---

## 📄 License

MIT — free to use, modify, and distribute.

---

👉 Want me to also add a **CHANGELOG entry** for `v1.5.1` that matches the style of your previous Franz releases (so it drops in clean with 1.4.5 and 1.5.0)?
