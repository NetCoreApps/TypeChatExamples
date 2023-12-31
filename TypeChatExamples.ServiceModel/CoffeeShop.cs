﻿using ServiceStack;
using ServiceStack.DataAnnotations;
using ServiceStack.Model;

namespace TypeChatExamples.ServiceModel;

[Icon(Svg = Icons.Category)]
public class Category
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string>? Temperatures { get; set; }
    public string? DefaultTemperature { get; set; }
    public List<string>? Sizes { get; set; }
    public string? DefaultSize { get; set; }
    public string? ImageUrl { get; set; }
    
    [Reference]
    public List<Product> Products { get; set; }

    [Reference]
    public List<CategoryOption> CategoryOptions { get; set; }
}

[Icon(Svg = Icons.Option)]
public class Option
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Type { get; set; }
    public List<string> Names { get; set; }
    public bool? AllowQuantity { get; set; }
    public string? QuantityLabel { get; set; }
}

[Icon(Svg = Icons.OptionQuantity)]
public class OptionQuantity
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
        
    public decimal Value { get; set; }
}

public class CategoryOption
{
    [AutoIncrement]
    public int Id { get; set; } 
    
    [References(typeof(Category))]
    public int CategoryId { get; set; }
    
    [References(typeof(Option))]
    public int OptionId { get; set; }
}

[Icon(Svg = Icons.Product)]
public class Product
{
    [AutoIncrement]
    public int Id { get; set; }
    
    [References(typeof(Category))]
    public int CategoryId { get; set; }

    public string Name { get; set; }

    [Reference]
    public Category Category { get; set; }

    public decimal Cost { get; set; }

    public string? ImageUrl { get; set; }
}

[Tag(Tags.CoffeeShop)]
[Route("/categories")]
public class QueryCategories : QueryDb<Category> {}
[Tag(Tags.CoffeeShop)]
public class CreateCategory : ICreateDb<Category>, IReturn<Category>
{
    public string Name { get; set; }
    public string Description { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Sizes { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Temperatures { get; set; }
    public string? DefaultSize { get; set; }
    public string? DefaultTemperature { get; set; }
    [Input(Type = "file"), UploadTo("products")]
    public string? ImageUrl { get; set; }
}
[Tag(Tags.CoffeeShop)]
public class UpdateCategory : IPatchDb<Category>, IReturn<Category>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Sizes { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Temperatures { get; set; }
    public string? DefaultSize { get; set; }
    public string? DefaultTemperature { get; set; }
    [Input(Type = "file"), UploadTo("products")]
    public string? ImageUrl { get; set; }

    [Input(Type = "hidden")]
    public List<int>? AddOptionIds { get; set; }
    [Input(Type = "hidden")]
    public List<int>? RemoveOptionIds { get; set; }
}

[Tag(Tags.CoffeeShop)]
public class DeleteCategory : IDeleteDb<Category>, IReturnVoid
{
    public int Id { get; set; }
}


[Tag(Tags.CoffeeShop)]
[Route("/products")]
public class QueryProducts : QueryDb<Product> {}
[Tag(Tags.CoffeeShop)]
public class CreateProduct : ICreateDb<Product>, IReturn<Product>
{
    public int CategoryId { get; set; }
    public string Name { get; set; }
    public decimal Cost { get; set; }
    [Input(Type = "file"), UploadTo("products")]
    public string? ImageUrl { get; set; }
}
[Tag(Tags.CoffeeShop)]
public class UpdateProduct : IPatchDb<Product>, IReturn<Product>
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }
    public string? Name { get; set; }
    public decimal? Cost { get; set; }
    [Input(Type = "file"), UploadTo("products")]
    public string? ImageUrl { get; set; }
}
[Tag(Tags.CoffeeShop)]
public class DeleteProduct : IDeleteDb<Product>, IReturnVoid
{
    public int Id { get; set; }
}

[Tag(Tags.CoffeeShop)]
[Route("/options")]
public class QueryOptions : QueryDb<Option> {}
[Tag(Tags.CoffeeShop)]
public class CreateOption : ICreateDb<Option>, IReturn<Option>
{
    public string Type { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string> Names { get; set; }
    public bool? AllowQuantity { get; set; }
    public string? QuantityLabel { get; set; }
}
[Tag(Tags.CoffeeShop)]
public class UpdateOption : IPatchDb<Option>, IReturn<Option>
{
    public int Id { get; set; }
    public string? Type { get; set; }
    [Input(Type = "tag"), FieldCss(Field = "col-span-12")]
    public List<string>? Names { get; set; }
    public bool? AllowQuantity { get; set; }
    public string? QuantityLabel { get; set; }
}
[Tag(Tags.CoffeeShop)]
public class DeleteOption : IDeleteDb<Option>, IReturnVoid
{
    public int Id { get; set; }
}

[Tag(Tags.CoffeeShop)]
[Route("/option-quantities")]
public class QueryOptionQuantities : QueryDb<OptionQuantity> {}
[Tag(Tags.CoffeeShop)]
public class CreateOptionQuantity : ICreateDb<OptionQuantity>, IReturn<OptionQuantity>
{
    public string Name { get; set; }
}
[Tag(Tags.CoffeeShop)]
public class UpdateOptionQuantity : IPatchDb<OptionQuantity>, IReturn<OptionQuantity>
{
    public int Id { get; set; }
    public string? Name { get; set; }
}
[Tag(Tags.CoffeeShop)]
public class DeleteOptionQuantity : IDeleteDb<OptionQuantity>, IReturnVoid
{
    public int Id { get; set; }
}

/* GPT Models */
public class Cart
{
    public List<LineItem> Items { get; set; }
}
public class LineItem
{
    public string Type { get; set; }
    public ProductItem Product { get; set; }
    public string Text { get; set; }
    public int Quantity { get; set; }
}
public class ProductItem
{
    public string Type { get; set; }
    public string Name { get; set; }
    public string Temperature { get; set; }
    public string Size { get; set; }
    public string OptionQuantity { get; set; }
}
public class ProductOption
{
    public string Type { get; set; }
    public string Name { get; set; }
}
