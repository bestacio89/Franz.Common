# **Franz.Common.Mapping**

*A lightweight, fast, and extensible object mapping library for the Franz ecosystem.*

**Current Version**: `1.5.4`

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

public ApplicationProfile()
        {
            // Book <-> BookDto
            CreateMap<Book, BookDto>()
                .ForMember(dest => dest.Isbn, opt => opt.MapFrom(src => src.Isbn.Value))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title.Value))
                .ForMember(dest => dest.Author, opt => opt.MapFrom(src => src.Author.Value))
                .ForMember(dest => dest.PublishedOn, opt => opt.MapFrom(src => src.PublishedOn))
                .ForMember(dest => dest.CopiesAvailable, opt => opt.MapFrom(src => src.CopiesAvailable))
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
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Name.Value))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value))
                .ForMember(dest => dest.BorrowedBooksCount, opt => opt.MapFrom(src => src.BorrowedBooks.Count))
                .ReverseMap()
                .ConstructUsing(dto => new Member(
                    new FullName(dto.FullName),
                    new Email(dto.Email)
                ));
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
services.AddAutoMapper(
            config => { }, // no extra config needed
            Assembly.GetExecutingAssembly()
        );

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