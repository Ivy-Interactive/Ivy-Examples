namespace MapsterExample
{
    public class MapsterConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Person, PersonDto>()
                .Map(dest => dest.FullName, src => string.IsNullOrEmpty(src.LastName) ? src.FirstName : $"{src.FirstName} {src.LastName}")
                .Map(dest => dest.IsAdult, src => src.Age >= 18)
                .Map(dest => dest.HasSingleWordName, src => !src.FirstName.Contains(' ') && !src.LastName.Contains(' '));

            config.NewConfig<PersonDto, Person>()
                .Map(dest => dest.Age, src => src.IsAdult ? 18 : 0)
                .AfterMapping((src, dest) =>
                {
                    var parts = src.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2 && src.HasSingleWordName)
                    {
                        dest.FirstName = parts[0];
                        dest.LastName = parts[1];
                    }
                    else 
                    {
                        dest.FirstName = src.FullName;
                        dest.LastName = "";
                    }
                });
        }
    }
}
