Perfect — this README is already clean, but with your new **constructor-aware, record-friendly mapper**, we can evolve it for **Franz.Common.Mapping 1.6.19** and showcase the architectural jump you just built.

Here’s the **updated README draft** (in your project’s tone and formatting):

---

# **Franz.Common.Mapping**

*A lightweight, fast, and extensible object mapping library for the Franz ecosystem.*

**Current Version**: 1.7.2
**Codename:** *Constructor-Aware Evolution*
- Part of the private **Franz Framework** ecosystem.
---

## 🌐 Overview

`Franz.Common.Mapping` is an **AutoMapper++ alternative** — type-safe, DI-friendly, and seamlessly integrated into the **Franz Framework**.
It now features **record-aware, constructor-smart mapping**, allowing pure immutable DTOs and records without any public parameterless constructors.

It provides:

* ⚡ **Simple by-name mapping** out of the box.
* 📑 **Profiles** with `CreateMap`, `ForMember`, `Ignore`, `ReverseMap`, and `ConstructUsing`.
* 🔧 **DI-friendly configuration** with `AddFranzMapping`.
* 🧠 **Constructor-aware instantiation** (auto-detects record positional constructors).
* 🏎 **Expression-based mapping** (fast, cached, reflection-minimal).
* 🧩 **Extendable** with custom converters and profiles.
* 🛠 **Assembly scanning** for automatic profile registration (`≥ 1.5.9`).

---

## 📦 Installation

```bash
dotnet add package Franz.Common.Mapping --version 1.6.19
```

---

## 🚀 Quick Start

### 1. Define a Profile

```csharp
using Franz.Common.Mapping.Core;

public class ApplicationProfile : FranzMapProfile
{
    public ApplicationProfile()
    {
        // Book ↔ BookDto
        CreateMap<Book, BookDto>()
            .ForMember(dest => dest.Isbn, src => src.Isbn.Value)
            .ForMember(dest => dest.Title, src => src.Title.Value)
            .ForMember(dest => dest.Author, src => src.Author.Value)
            .ReverseMap()
            .ConstructUsing(dto => new Book(
                new ISBN(dto.Isbn),
                new Title(dto.Title),
                new Author(dto.Author)
            ));

        // Member ↔ MemberDto (record-friendly)
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

```csharp
services.AddFranzMapping(Assembly.GetExecutingAssembly());
```

or inline:

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

var member = new Member(new FullName("John Doe"), new Email("john@acme.com"));
var dto = mapper.Map<Member, MemberDto>(member);

Console.WriteLine(dto.FullName); // John Doe
Console.WriteLine(dto.Email);    // john@acme.com
```

---
### Version 1.6.20
- Updated to **.NET 10.0**


## ✨ New in 1.6.19

### 🧠 **Constructor-Aware Mapping Engine**

* Detects and invokes **record positional constructors** automatically.
* Eliminates the need for `public MemberDto() { }`.
* Allows **immutable DTOs and record structs** out-of-the-box.
* Falls back to `Activator.CreateInstance()` only when no usable constructor exists.
* 100 % backward-compatible with `ConstructUsing()` and legacy mappings.

### 🧩 **Architectural Impact**

* Strengthens immutability and contract integrity in the Franz ecosystem.
* Enables the “DTOs must be immutable” Tribunal rule to pass naturally.
* Outperforms AutoMapper in instantiation efficiency and architectural compliance.

---

## 🧩 Features Summary

* 🔄 **By-name mapping** fallback (zero config).
* 🎯 **Profiles** for explicit control.
* 🛠 **ForMember / Ignore / ConstructUsing** API.
* 💾 **Immutable, cached mapping expressions**.
* 🧩 **Dependency Injection integration**.
* 🔍 **Assembly scanning** for auto-profile discovery.
* 🧠 **Record-friendly smart instantiation**.
* ✅ **Tested in the Franz Book & Library APIs** for production stability.

---

## 🛣 Roadmap

* 🔌 Custom type converters (`ITypeConverter<TSource, TDest>`).
* ⏳ Async mapping support for I/O-heavy scenarios.
* 🚨 Startup validation (fail-fast if mappings incomplete).
* 🧩 Expression caching for constructor binding.
* 🧑‍💻 Roslyn analyzers to detect missing profiles at compile time.

---

## 📜 License

MIT — free to use, modify, and distribute.

---

Would you like me to also generate the matching `CHANGELOG.md` entry for `1.6.19` in your Franz release format (with version header, bullet evolution notes, and commit tag)?
