using AutoMapper;

namespace Eduva.Application.Common.Mappings
{
    public static class AppMapper
    {
        private readonly static Lazy<IMapper> _lazy = new Lazy<IMapper>(() =>
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AppMappingProfile>();
            });
            return config.CreateMapper();
        });

        public static IMapper Mapper => _lazy.Value;
    }
}
