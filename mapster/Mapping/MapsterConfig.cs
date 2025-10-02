using Ivy.Mapster.Models;
using Mapster;

namespace Ivy.Mapster.Mapping
{
    public class MapsterConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<Person, PersonDto>()
                .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
                .Map(dest => dest.IsAdult, src => src.Age >= 18);

            config.NewConfig<PersonDto, Person>()
                .Map(dest => dest.FirstName,
                     src => src.FullName)
                .Map(dest => dest.Age,
                     src => src.IsAdult ? 18 : 0);
        }
    }
}
