using Microsoft.EntityFrameworkCore;
using TouristGuide.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    var cityData = new[]
    {
        new
        {
            Id = 1,
            Name = "Казань",
            Region = "Республика Татарстан",
            Population = 1310000,
            ShortDescription = "Один из крупнейших культурных центров России.",
            History = "Казань - исторический город с уникальным сочетанием татарских и русских традиций.",
            CoatOfArmsUrl = "/images/coats/kazan.png",
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/b/bd/%D0%94%D0%B2%D0%BE%D1%80%D0%B5%D1%86_%D0%B7%D0%B5%D0%BC%D0%BB%D0%B5%D0%B4%D0%B5%D0%BB%D1%8C%D1%86%D0%B5%D0%B22.jpg"
        },
        new
        {
            Id = 2,
            Name = "Сочи",
            Region = "Краснодарский край",
            Population = 466000,
            ShortDescription = "Курортный город на Черном море, известный горами и пляжами.",
            History = "Сочи - крупный российский курортный город, принимавший зимние Олимпийские игры 2014 года.",
            CoatOfArmsUrl = "/images/coats/sochi.png",
            ImageUrl = "/images/sochi.webp"
        },
        new
        {
            Id = 3,
            Name = "Екатеринбург",
            Region = "Свердловская область",
            Population = 1490000,
            ShortDescription = "Крупный город на границе Европы и Азии.",
            History = "Екатеринбург - важный промышленный и культурный центр Урала.",
            CoatOfArmsUrl = "/images/coats/yekaterinburg.png",
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/3/39/E-burg_asv2019-05_img46_view_from_VysotSky.jpg"
        }
    };

    foreach (var item in cityData)
    {
        var city = dbContext.Cities.FirstOrDefault(entity => entity.Id == item.Id);
        if (city is null)
        {
            continue;
        }

        city.Name = item.Name;
        city.Region = item.Region;
        city.Population = item.Population;
        city.ShortDescription = item.ShortDescription;
        city.History = item.History;
        city.CoatOfArmsUrl = item.CoatOfArmsUrl;
        city.ImageUrl = item.ImageUrl;
    }

    var attractionData = new[]
    {
        new
        {
            Id = 1,
            Name = "Казанский кремль",
            ShortDescription = "Объект ЮНЕСКО и главный символ Казани.",
            History = "Казанский кремль включает исторические здания разных эпох.",
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/b/b3/%D0%9A%D0%B0%D0%B7%D0%B0%D0%BD%D1%81%D0%BA%D0%B8%D0%B9_%D0%BA%D1%80%D0%B5%D0%BC%D0%BB%D1%8C._%D0%9F%D0%B0%D0%BD%D0%BE%D1%80%D0%B0%D0%BC%D0%B0_%D1%81_%D0%BA%D0%BE%D0%BB%D0%B5%D1%81%D0%B0_%D0%BE%D0%B1%D0%BE%D0%B7%D1%80%D0%B5%D0%BD%D0%B8%D1%8F.jpg",
            OpeningHours = "10:00 - 18:00",
            VisitCost = "Вход на территорию бесплатный, музеи - по билетам"
        },
        new
        {
            Id = 2,
            Name = "Улица Баумана",
            ShortDescription = "Популярная пешеходная улица с кафе и исторической архитектурой.",
            History = "Улица Баумана давно является центральной прогулочной и торговой улицей Казани.",
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/8/8c/Baumana_Street_Kazan_Russia_2009_sept_06.jpg",
            OpeningHours = "Круглосуточно",
            VisitCost = "Бесплатно"
        },
        new
        {
            Id = 3,
            Name = "Сочинский дендрарий",
            ShortDescription = "Крупный субтропический парк с панорамными видами.",
            History = "Основан в конце XIX века, парк известен редкими растениями.",
            ImageUrl = "https://images.unsplash.com/photo-1501785888041-af3ef285b470?auto=format&fit=crop&w=1200&q=80",
            OpeningHours = "09:00 - 20:00",
            VisitCost = "Платно, стоимость зависит от сезона"
        },
        new
        {
            Id = 4,
            Name = "Роза Хутор",
            ShortDescription = "Горный курорт с активностями круглый год.",
            History = "Роза Хутор получил международную известность во время зимней Олимпиады.",
            ImageUrl = "/images/rosa-hutor.jpeg",
            OpeningHours = "08:30 - 22:00",
            VisitCost = "Платно"
        },
        new
        {
            Id = 5,
            Name = "Храм на Крови",
            ShortDescription = "Памятный храм и важный исторический объект.",
            History = "Построен на месте, связанном с последними днями семьи Романовых.",
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/f/f1/Yekaterinburg_cathedral_on_the_blood_2007.jpg",
            OpeningHours = "08:00 - 19:00",
            VisitCost = "Бесплатно"
        },
        new
        {
            Id = 6,
            Name = "БЦ Высоцкий",
            ShortDescription = "Небоскреб со смотровой площадкой на город.",
            History = "Современная достопримечательность Екатеринбурга с панорамными видами.",
            ImageUrl = "https://upload.wikimedia.org/wikipedia/commons/b/bb/E-burg_asv2019-05_img58_VysotSky.jpg",
            OpeningHours = "10:00 - 22:00",
            VisitCost = "Платно"
        }
    };

    foreach (var item in attractionData)
    {
        var attraction = dbContext.Attractions.FirstOrDefault(entity => entity.Id == item.Id);
        if (attraction is null)
        {
            continue;
        }

        attraction.Name = item.Name;
        attraction.ShortDescription = item.ShortDescription;
        attraction.History = item.History;
        attraction.ImageUrl = item.ImageUrl;
        attraction.OpeningHours = item.OpeningHours;
        attraction.VisitCost = item.VisitCost;
    }

    dbContext.SaveChanges();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cities}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
