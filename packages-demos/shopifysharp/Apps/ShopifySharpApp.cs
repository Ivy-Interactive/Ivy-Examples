namespace ShopifySharpExample;

[App(icon: Icons.ShoppingBag, title: "ShopifySharp")]
public class ShopifySharpApp : ViewBase
{
    public override object? Build()
    {
        var shopDomain = this.UseState<string>(() => "");
        var accessToken = this.UseState<string>(() => "");
        var products = this.UseState<Product[]?>(() => null);
        var isLoading = this.UseState(false);
        var error = this.UseState<string?>(() => null);
        var sortKey = this.UseState<string>(() => "TITLE");
        var sortDirection = this.UseState<string>(() => "ASC");
        var queryFilter = this.UseState<string>(() => "");

        this.UseEffect(async () =>
        {
            var productsLoaded = products.Value is not null;
            if (!productsLoaded || isLoading.Value)
            {
                return;
            }

            await LoadProducts();
        }, sortKey, sortDirection, queryFilter);

        async Task LoadProducts()
        {
            if (string.IsNullOrWhiteSpace(shopDomain.Value) || string.IsNullOrWhiteSpace(accessToken.Value))
            {
                error.Value = "Please fill in both domain and access token fields";
                return;
            }

            try
            {
                isLoading.Value = true;
                error.Value = null;
                var graph = new GraphService(shopDomain.Value, accessToken.Value);
                var query = @"query ($first: Int!, $sortKey: ProductSortKeys, $reverse: Boolean, $query: String) {
                  products(first: $first, sortKey: $sortKey, reverse: $reverse, query: $query) {
                    nodes {
                      title
                      images(first: 1) { edges { node { url src } } }
                      variants(first: 1) { edges { node { price } } }
                    }
                  }
                }";

                var variables = new Dictionary<string, object>
                {
                    ["first"] = 250,
                    ["reverse"] = string.Equals(sortDirection.Value, "DESC", StringComparison.OrdinalIgnoreCase)
                };

                if (!string.IsNullOrWhiteSpace(sortKey.Value))
                {
                    variables["sortKey"] = sortKey.Value;
                }

                if (!string.IsNullOrWhiteSpace(queryFilter.Value))
                {
                    variables["query"] = queryFilter.Value;
                }

                var request = new GraphRequest
                {
                    Query = query,
                    Variables = variables
                };
                var response = await graph.PostAsync<System.Text.Json.JsonDocument>(request);

                System.Text.Json.JsonElement itemsElement = default;
                bool itemsAreEdges = false;
                if (response?.Data is not null)
                {
                    var root = response.Data.RootElement;
                    if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        System.Text.Json.JsonElement productsEl;
                        if (root.TryGetProperty("products", out productsEl))
                        {
                            // Data already points at the GraphQL 'data' object
                        }
                        else if (root.TryGetProperty("data", out var dataEl) && dataEl.TryGetProperty("products", out productsEl))
                        {
                            // Some serializers may keep the envelope; handle just in case
                        }
                        else
                        {
                            productsEl = default;
                        }

                        if (productsEl.ValueKind != System.Text.Json.JsonValueKind.Undefined && productsEl.ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            if (productsEl.TryGetProperty("nodes", out var nodesEl))
                            {
                                itemsElement = nodesEl;
                                itemsAreEdges = false;
                            }
                            else if (productsEl.TryGetProperty("edges", out var edgesEl))
                            {
                                itemsElement = edgesEl;
                                itemsAreEdges = true;
                            }
                        }
                    }
                }

                var mapped = itemsElement.ValueKind == System.Text.Json.JsonValueKind.Array
                    ? itemsElement.EnumerateArray().Select(itemEl =>
                    {
                        var nodeEl = itemsAreEdges && itemEl.TryGetProperty("node", out var n) ? n : (itemsAreEdges ? default : itemEl);
                        var title = nodeEl.ValueKind == System.Text.Json.JsonValueKind.Object && nodeEl.TryGetProperty("title", out var t) ? t.GetString() : null;

                        string imageUrl = string.Empty;
                        if (nodeEl.TryGetProperty("images", out var imagesEl)
                            && imagesEl.TryGetProperty("edges", out var iEdgesEl))
                        {
                            var ie = iEdgesEl.EnumerateArray();
                            if (ie.MoveNext())
                            {
                                var imageNode = ie.Current.TryGetProperty("node", out var inEl) ? inEl : default;
                                if (imageNode.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    if (imageNode.TryGetProperty("url", out var urlEl)) imageUrl = urlEl.GetString() ?? string.Empty;
                                    if (string.IsNullOrWhiteSpace(imageUrl) && imageNode.TryGetProperty("src", out var srcEl)) imageUrl = srcEl.GetString() ?? string.Empty;
                                }
                            }
                        }

                        decimal price = 0m;
                        if (nodeEl.TryGetProperty("variants", out var variantsEl)
                            && variantsEl.TryGetProperty("edges", out var vEdgesEl))
                        {
                            var ve = vEdgesEl.EnumerateArray();
                            if (ve.MoveNext())
                            {
                                var vNode = ve.Current.TryGetProperty("node", out var vn) ? vn : default;
                                if (vNode.ValueKind == System.Text.Json.JsonValueKind.Object
                                    && vNode.TryGetProperty("price", out var priceEl))
                                {
                                    var priceStr = priceEl.GetString();
                                    if (!string.IsNullOrWhiteSpace(priceStr))
                                    {
                                        decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out price);
                                    }
                                }
                            }
                        }

                        return new Product
                        {
                            Title = title,
                            Images = string.IsNullOrWhiteSpace(imageUrl) ? null : new List<ProductImage> { new ProductImage { Src = imageUrl } },
                            Variants = new List<ProductVariant> { new ProductVariant { Price = price } }
                        };
                    }).ToArray()
                    : Array.Empty<Product>();

                products.Value = mapped;
            }
            catch (ShopifyException ex)
            {
                error.Value = $"Shopify error: {ex.Message}";
            }
            catch (Exception ex)
            {
                error.Value = ex.Message;
            }
            finally
            {
                isLoading.Value = false;
            }
        }

        object productCard(Product p)
        {
            var imageUrl = p.Images?.FirstOrDefault()?.Src ?? "";
            var price = p.Variants?.FirstOrDefault()?.Price ?? 0m;
            var imageSource = string.IsNullOrWhiteSpace(imageUrl)
                ? "https://placehold.co/200x200?text=No+Image"
                : imageUrl;

            return new Card(
                Layout.Vertical().Gap(3)
                | Text.H4(p.Title ?? "Untitled")
                | (Layout.Center()
                    | new Image(imageSource))
                | Text.Block(price > 0 ? $"{price:C}" : "")
            );
        }

        var searchControlsDisabled = products.Value is null || isLoading.Value;

        var header = Layout.Vertical().Gap(3)
            | Text.H3("Shopify Product Explorer")
            | Text.Muted("Connect to your Shopify store, fetch the full product catalog, and refine the results using the filters below.")
            | Text.Label("Domain")
            | shopDomain.ToTextInput().Placeholder("Domain (e.g.: example.myshopify.com)")
            | Text.Label("Access Token")
            | accessToken.ToTextInput().Placeholder("Access Token")
            | Text.Label("Sort by")
            | sortKey.ToSelectInput(new[] { "TITLE", "CREATED_AT", "UPDATED_AT", "ID" }.ToOptions()).Placeholder("Sort by").Disabled(searchControlsDisabled)
            | Text.Label("Sort direction")
            | sortDirection.ToSelectInput(new[] { "ASC", "DESC" }.ToOptions()).Placeholder("Sort direction").Disabled(searchControlsDisabled)
            | Text.Label("Filter query")
            | queryFilter.ToTextInput().Placeholder("Filter query (e.g. tag:'summer')").Variant(TextInputs.Search).Disabled(searchControlsDisabled)
            | new Button("Get Products", onClick: async _ => await LoadProducts()).Disabled(isLoading.Value).Width(Size.Full())
            | Text.Small("This demo uses ShopifySharp library to fetch the full product catalog from your Shopify store.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework) and [ShopifySharp](https://github.com/nozzlegear/ShopifySharp)")
            ;

        object body;
        if (error.Value != null)
        {
            body = new Card(body = (Layout.Center() | Text.Block("Error: " + error.Value)));
        }
        else if (isLoading.Value)
        {
            body = new Card(Layout.Center() | Text.Block("Loading products..."));
        }
        else if (products.Value == null)
        {
            body = new Card(Layout.Center() | Text.Block("Click the button above to view products"));
        }
        else if (products.Value.Length == 0)
        {
            body = new Card(Layout.Center() | Text.Block("No products found."));
        }
        else
        {
            body = new Card(
                Layout.Vertical()
                | Text.H3("Products")
                | Text.Muted("Browse all available products here. Use the filters on the left panel to refine your search."),

                Layout.Grid().Columns(3)
                | products.Value.Select(productCard).ToArray()
                ).Height(Size.Fit().Min(Size.Full()));
        }

        return Layout.Horizontal().Gap(4)
               | new Card(header).Width(Size.Fraction(0.4f)).Height(Size.Fit().Min(Size.Full()))
               | body;
    }
    // GraphQL helper DTOs
    private sealed class GraphQlResponse<T>
    {
        public T? Data { get; set; }
        public IEnumerable<GraphQlError>? Errors { get; set; }
    }

    private sealed class GraphQlError
    {
        public string? Message { get; set; }
    }

    private sealed class ProductsData
    {
        public Connection<ProductNode>? Products { get; set; }
    }

    private sealed class Connection<TNode>
    {
        public List<Edge<TNode>>? Edges { get; set; }
    }

    private sealed class Edge<TNode>
    {
        public TNode? Node { get; set; }
    }

    private sealed class ProductNode
    {
        public string? Title { get; set; }
        public Connection<ImageNode>? Images { get; set; }
        public Connection<VariantNode>? Variants { get; set; }
    }

    private sealed class ImageNode
    {
        public string? Url { get; set; }
        public string? Src { get; set; }
    }

    private sealed class VariantNode
    {
        public MoneyV2? Price { get; set; }
    }

    private sealed class MoneyV2
    {
        public string? Amount { get; set; }
    }
}