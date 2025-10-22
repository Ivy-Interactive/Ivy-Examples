namespace DataTablesDemo.Apps;

[App(icon: Icons.Table, title: "DataTables Demo")]
public class DataTablesDemoApp : ViewBase
{
    public override object? Build()
    {
        var selectedDemo = this.UseState(0);
        
        var demos = new[]
        {
            ("Basic DataTable", "Simple table with sample data"),
            ("Sortable Columns", "Table with sortable columns"),
            ("Filterable Data", "Table with filtering capabilities"),
            ("Large Dataset", "Performance demo with 10,000+ rows"),
            ("Custom Columns", "Table with custom column types and formatting"),
            ("Interactive Demo", "Live data manipulation with add/remove functionality")
        };
        
        var demoMenuItems = demos
            .Select((demo, idx) => MenuItem.Default(demo.Item1).HandleSelect(() => selectedDemo.Value = idx))
            .ToArray();
        
        var demoDropdown = new Button(demos[selectedDemo.Value].Item1)
            .Primary()
            .Icon(Icons.ChevronDown)
            .WithDropDown(demoMenuItems);
        
        var description = Text.Muted(demos[selectedDemo.Value].Item2);
        
        var tableContent = selectedDemo.Value switch
        {
            0 => BuildBasicDataTable(),
            1 => BuildSortableDataTable(),
            2 => BuildFilterableDataTable(),
            3 => BuildLargeDatasetTable(),
            4 => BuildCustomColumnsTable(),
            5 => BuildInteractiveDemo(),
            _ => BuildBasicDataTable()
        };
        
        return Layout.Vertical().Gap(4).Padding(2)
            | Text.H1("Ivy DataTables Demo")
            | Text.Block("Explore the powerful DataTable widget capabilities with different data scenarios.")
            | new Separator()
            
            | Layout.Horizontal().Gap(4).Align(Align.Center)
                | demoDropdown
                | description
            
            | new Separator()
            | tableContent
            
            | new Separator()
            | Text.Small("This demo showcases Ivy Framework's DataTable widget built on Apache Arrow for high performance.")
            | Text.Markdown("Built with [Ivy Framework](https://github.com/Ivy-Interactive/Ivy-Framework)");
    }
    
    private object BuildBasicDataTable()
    {
        var sampleData = GenerateSampleUsers(50);
        
        return new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Basic DataTable")
            | Text.Muted("Simple table displaying user data with automatic column detection")
            | sampleData.ToDataTable()
                .Header(u => u.Name, "Full Name")
                .Header(u => u.Email, "Email Address")
                .Header(u => u.Salary, "Salary")
                .Header(u => u.Status, "Status")
                .Header(u => u.IsActive, "Active")
        );
    }
    
    private object BuildSortableDataTable()
    {
        var sampleData = GenerateSampleUsers(100);
        
        return new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Sortable DataTable")
            | Text.Muted("Click column headers to sort data")
            | sampleData.ToDataTable()
                .Header(u => u.Name, "Full Name")
                .Header(u => u.Email, "Email Address")
                .Header(u => u.Salary, "Salary")
                .Header(u => u.Status, "Status")
                .Header(u => u.IsActive, "Active")
                .Sortable(u => u.Name, true)
                .Sortable(u => u.Email, true)
                .Sortable(u => u.Salary, true)
        );
    }
    
    private object BuildFilterableDataTable()
    {
        var sampleData = GenerateSampleUsers(200);
        
        return new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Filterable DataTable")
            | Text.Muted("Use the search box to filter data across all columns")
            | sampleData.ToDataTable()
                .Header(u => u.Name, "Full Name")
                .Header(u => u.Email, "Email Address")
                .Header(u => u.Salary, "Salary")
                .Header(u => u.Status, "Status")
                .Header(u => u.IsActive, "Active")
                .Filterable(u => u.Name, true)
                .Filterable(u => u.Email, true)
        );
    }
    
    private object BuildLargeDatasetTable()
    {
        var sampleData = GenerateSampleUsers(10000);
        
        return new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Large Dataset Performance")
            | Text.Muted("Demonstrates high performance with 10,000+ rows using Apache Arrow")
            | sampleData.ToDataTable()
                .Header(u => u.Name, "Full Name")
                .Header(u => u.Email, "Email Address")
                .Header(u => u.Salary, "Salary")
                .Header(u => u.Status, "Status")
                .Header(u => u.IsActive, "Active")
                .Sortable(u => u.Name, true)
                .Filterable(u => u.Name, true)
        );
    }
    
    private object BuildCustomColumnsTable()
    {
        var sampleData = GenerateSampleUsers(75);
        
        return new Card(
            Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Custom Columns")
            | Text.Muted("Table with custom column formatting and types")
            | sampleData.ToDataTable()
                .Header(u => u.Name, "Full Name")
                .Header(u => u.Email, "Email Address")
                .Header(u => u.Salary, "Salary")
                .Header(u => u.Status, "Status")
                .Header(u => u.IsActive, "Active")
                .Sortable(u => u.Name, true)
                .Filterable(u => u.Name, true)
        );
    }
    
    private object BuildInteractiveDemo()
    {
        var users = this.UseState(GenerateSampleUsers(10).ToList());
        var newUserName = this.UseState("");
        var newUserEmail = this.UseState("");
        var newUserSalary = this.UseState(50000);
        
        var addUser = () =>
        {
            if (!string.IsNullOrEmpty(newUserName.Value) && !string.IsNullOrEmpty(newUserEmail.Value))
            {
                var newUser = new User
                {
                    Id = users.Value.Count + 1000,
                    Name = newUserName.Value,
                    Email = newUserEmail.Value,
                    Salary = newUserSalary.Value,
                    Status = Icons.Star,
                    IsActive = true
                };
                
                users.Value.Add(newUser);
                users.Set([.. users.Value]);
                
                newUserName.Value = "";
                newUserEmail.Value = "";
                newUserSalary.Value = 50000;
            }
        };
        
        var removeUser = (User user) =>
        {
            users.Value.Remove(user);
            users.Set([.. users.Value]);
        };
        
        return Layout.Vertical().Gap(4).Padding(2)
            | Text.H2("Interactive DataTable Demo")
            | Text.Muted("Add and remove users dynamically to see real-time updates")
            
            | new Card(
                Layout.Vertical().Gap(4).Padding(2)
                | Text.H3("Add New User")
                | Layout.Horizontal().Gap(2)
                    | newUserName.ToInput(placeholder: "Full Name")
                    | newUserEmail.ToInput(placeholder: "Email")
                    | newUserSalary.ToInput(placeholder: "Salary")
                    | new Button("Add User", addUser).Primary()
            )
            
            | users.Value.AsQueryable().ToDataTable()
                .Header(u => u.Name, "Full Name")
                .Header(u => u.Email, "Email Address")
                .Header(u => u.Salary, "Salary")
                .Header(u => u.Status, "Status")
                .Header(u => u.IsActive, "Active")
                .Sortable(u => u.Name, true)
                .Filterable(u => u.Name, true)
            
            | Text.Small($"Total Users: {users.Value.Count}");
    }
    
    private IQueryable<User> GenerateSampleUsers(int count)
    {
        var firstNames = new[] { "John", "Sarah", "Mike", "Emily", "Alex", "Lisa", "David", "Jessica", "Robert", "Amanda", "Kevin", "Michelle", "Christopher", "Jennifer", "Daniel", "Nicole", "Matthew", "Stephanie", "Andrew", "Rachel", "James", "Patricia", "Thomas", "Barbara", "Charles", "Susan", "Joseph", "Linda", "Paul", "Karen", "Mark", "Betty", "Donald", "Helen", "Steven", "Dorothy", "Kenneth", "Sandra", "Brian", "Ashley", "Edward", "Kimberly", "Ronald", "Donna", "Anthony", "Carol", "Ruth", "Jason", "Sharon", "Nancy", "Larry", "Frank", "Diane", "Carl", "Janet", "Gerald", "Judith", "Harold", "Teresa", "Dennis", "Pamela", "Eugene", "Gloria", "Arthur", "Doris", "Ralph", "Cheryl", "Russell", "Mildred", "Henry", "Katherine", "Willie", "Joan", "Albert", "Evelyn", "Howard", "Virginia", "Craig", "Melissa", "Alan", "Debra", "Louis", "Rebecca", "Billy", "Laura", "Terry", "Anna", "Sean", "Marie", "Joe", "Frances", "Ann" };
        var lastNames = new[] { "Smith", "Johnson", "Brown", "Davis", "Wilson", "Chen", "Miller", "Taylor", "Garcia", "White", "Lee", "Rodriguez", "Martinez", "Lopez", "Anderson", "Thompson", "Jackson", "Harris", "Clark", "Lewis", "Walker", "Hall", "Allen", "Young", "King", "Wright", "Scott", "Green", "Baker", "Adams", "Nelson", "Carter", "Mitchell", "Perez", "Roberts", "Turner", "Phillips", "Campbell", "Parker", "Evans", "Edwards", "Collins", "Stewart", "Sanchez", "Morris", "Rogers", "Reed", "Cook", "Morgan", "Bell", "Murphy", "Bailey", "Rivera", "Cooper", "Richardson", "Cox", "Howard", "Ward", "Torres", "Peterson", "Gray", "Ramirez", "James", "Watson", "Brooks", "Kelly", "Sanders", "Price", "Bennett", "Wood", "Barnes", "Ross", "Henderson", "Coleman", "Jenkins", "Perry", "Powell", "Long", "Patterson", "Hughes", "Flores", "Washington", "Butler", "Simmons", "Foster", "Gonzales", "Bryant", "Alexander", "Russell", "Griffin", "Diaz", "Hayes", "Myers", "Ford", "Hamilton", "Graham", "Sullivan", "Wallace", "Woods", "Cole" };
        var statusIcons = new[] { Icons.Rocket, Icons.Star, Icons.ThumbsUp, Icons.Heart, Icons.Check, Icons.Clock, Icons.X, Icons.Circle };
        
        return Enumerable.Range(0, count).Select(id =>
        {
            var random = new Random(id * 17 + 42); // Different seed per row
            var firstName = firstNames[random.Next(firstNames.Length)];
            var lastName = lastNames[random.Next(lastNames.Length)];
            var name = $"{firstName} {lastName}";
            var email = $"{firstName.ToLower()}.{lastName.ToLower()}{id}@example.com";
            var salary = random.Next(40000, 150000);
            var status = statusIcons[random.Next(statusIcons.Length)];
            var isActive = random.Next(100) > 25;
            
            return new User
            {
                Id = id,
                Name = name,
                Email = email,
                Salary = salary,
                Status = status,
                IsActive = isActive
            };
        }).AsQueryable();
    }
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Salary { get; set; }
    public Icons Status { get; set; }
    public bool IsActive { get; set; }
}
