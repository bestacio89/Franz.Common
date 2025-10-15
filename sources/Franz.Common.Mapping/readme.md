# **Franz.Common.Mapping**

*A lightweight, fast, and extensible object mapping library for the Franz ecosystem.*

**Current Version**: `1.6.14`

---

## 🌐 Overview

`Franz.Common.Mapping` is designed as an **AutoMapper++ alternative** — type-safe, DI-friendly, and integrated with the **Franz Framework**.
It provides:

* ⚡ **Simple by-name mapping** out of the box.
* 📑 **Profiles** with `CreateMap`, `ForMember`, `Ignore`, `ReverseMap`, and `ConstructUsing`.
* 🔧 **Configurable via DI** with `AddFranzMapping`.
* 🏎 **Expression-based mapping** (fast, cached, reflection-minimal).
* 🧩 **Extendable** with custom converters and profiles.
* 🛠 **Assembly scanning** for automatic profile registration (new in `1.5.9`).

---

## 📦 Installation

```bash
dotnet add package Franz.Common.Mapping --version 1.6.14
```

---

## 🚀 Quick Start

### 1. Define a Profile

```csharp
using Franz.Common.Mapping.Core;
using Franz.Common.Mapping.Profiles;

public class ApplicationProfile : FranzMapProfile
{
    public ApplicationProfile()
    {
        // Book <-> BookDto
        CreateMap<Book, BookDto>()
            .ForMember(dest => dest.Isbn, src => src.Isbn.Value)
            .ForMember(dest => dest.Title, src => src.Title.Value)
            .ForMember(dest => dest.Author, src => src.Author.Value)
            .ForMember(dest => dest.PublishedOn, src => src.PublishedOn)
            .ForMember(dest => dest.CopiesAvailable, src => src.CopiesAvailable)
            .ReverseMap()
            .ConstructUsing(dto => new Book(
                new ISBN(dto.Isbn),
                new Title(dto.Title),
                new Author(dto.Author),
                dto.PublishedOn,
                dto.CopiesAvailable
            ));

        // Member <-> MemberDto
        CreateMap<Member, MemberDto>()
            .ForMember(dest => dest.FullName, src => src.Name.Value)
            .ForMember(dest => dest.Email, src => src.Email.Value)
            .ForMember(dest => dest.BorrowedBooksCount, src => src.BorrowedBooks.Count)
            .ReverseMap()
            .ConstructUsing(dto => new Member(
                new FullName(dto.FullName),
                new Email(dto.Email)
            ));
    }
}
```

---

### 2. Register in DI

#### Assembly scanning (recommended, new in `1.5.9`):

```csharp
services.AddFranzMapping(Assembly.GetExecutingAssembly());
```

#### Inline + assemblies:

```csharp
services.AddFranzMapping(cfg =>
{
    cfg.CreateMap<Foo, Bar>();
}, Assembly.GetExecutingAssembly());
```

---

### 3. Use the Mapper

```csharp
var mapper = provider.GetRequiredService<IFranzMapper>();

var book = new Book { Name = "The Hobbit", Isbn = "978-0261103344" };
var dto = mapper.Map<Book, BookDto>(book);

Console.WriteLine(dto.Title); // "The Hobbit"
Console.WriteLine(dto.Isbn);  // "978-0261103344"
```

---

## ✨ Features

* 🔄 **By-name mapping** fallback (zero config).
* 🎯 **Profiles** for explicit control.
* 🛠 **ForMember & Ignore** API.
* 💾 **Immutable, cached mapping expressions** for performance.
* 🧩 **DI integration** with `AddFranzMapping`.
* 🔍 **Assembly scanning** for auto-discovery of profiles.
* ✅ **Tested in the Franz Book API** to ensure real-world readiness.

---

## 🛣 Roadmap

* 🔌 Custom type converters (`ITypeConverter<TSource, TDest>`).
* ⏳ Async mapping support for I/O-heavy scenarios.
* 🚨 Startup validation (fail-fast if mappings are incomplete).
* 🧑‍💻 Roslyn analyzers to catch missing profiles at compile time.

---

## 📜 License

MIT — free to use, modify, and distribute.

