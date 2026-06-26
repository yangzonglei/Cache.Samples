using Yzl.Net.Extensions.Cache.Samples.Services;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// 注册缓存框架（核心步骤）
// ===================================================================
//
// AddEnableCaching 方法会完成以下工作：
//   1. 注册 MemoryCache（内存缓存提供器）
//   2. 如果 enableRedis = true，注册 RedisCacheProvider
//   3. 注册缓存拦截器（Castle DynamicProxy）
//   4. 注册缓存操作处理器（Cacheable / CachePut / CacheEvict）
//   5. 自动扫描程序集，为标注了缓存注解的类创建动态代理
//
// 参数说明：
//   assemblies: null           → 自动扫描当前应用程序域的所有程序集
//   enableRedis: false         → 不启用 Redis（仅使用内存缓存）
//   enableRedis: true          → 启用 Redis（需配置连接字符串）
//   redisConnectionString      → Redis 连接字符串，从配置文件中读取
// ===================================================================

// 方案 A：仅使用内存缓存（无需 Redis）
// builder.Services.AddEnableCaching(assemblies: null, enableRedis: false);

// 方案 B：启用 Redis 缓存（如已配置 Redis 连接字符串，取消下面注释）
// var redisConn = builder.Configuration.GetConnectionString("Redis");
// builder.Services.AddEnableCaching(assemblies: null, enableRedis: true, redisConnectionString: redisConn);

// 方案 C（推荐）：根据配置文件动态决定是否启用 Redis
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConn))
{
    builder.Services.AddEnableCaching(assemblies: null, enableRedis: true, redisConnectionString: redisConn);
    Console.WriteLine("✓ Redis 缓存已启用");
}
else
{
    builder.Services.AddEnableCaching(assemblies: null, enableRedis: false);
    Console.WriteLine("✓ 内存缓存已启用（如需 Redis，请在 appsettings.Development.json 中配置 ConnectionStrings:Redis）");
}

// ===================================================================
// 注册示例服务
// ===================================================================
//
// 注意：使用缓存注解的服务必须满足以下条件之一：
//   条件 A：方法标记为 virtual（虚方法）—— Castle DynamicProxy 通过继承创建代理
//   条件 B：类实现了接口（interface）—— 通过接口代理
//
// 生命周期建议使用 Transient 或 Scoped：
//   - Transient：每次请求创建新实例，代理自动包装
//   - Singleton：需确保代理的单例性，通常由 AddEnableCaching 内部处理
// ===================================================================
builder.Services.AddTransient<BasicCacheService>();
builder.Services.AddTransient<CacheLifecycleService>();
builder.Services.AddTransient<SpelKeyService>();
builder.Services.AddTransient<ConditionalService>();
builder.Services.AddTransient<ConfigInheritanceService>();
builder.Services.AddTransient<AsyncCacheService>();
builder.Services.AddTransient<SlidingExpirationService>();
builder.Services.AddTransient<RedisCacheService>();
builder.Services.AddTransient<CacheEvictAllService>();

// 注册控制器
builder.Services.AddControllers();

// ===================================================================
// 配置 CORS（允许跨域调试）
// ===================================================================
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors();
app.MapControllers();

// ===================================================================
// 启动信息
// ===================================================================
Console.WriteLine("""

                  ╔══════════════════════════════════════════════════════════════╗
                  ║        Yzl.Net.Extensions.Cache.Samples                     ║
                  ║                                                              ║
                  ║  📚 示例导航: http://localhost:17005/api/samples              ║
                  ║  📊 调用统计: http://localhost:17005/api/samples/stats       ║
                  ║                                                              ║
                  ║  提示：建议按照 README.md 中的章节顺序依次测试。            ║
                  ║  每个端点返回的信息包含：                                    ║
                  ║    - 数据结果                                               ║
                  ║    - 执行耗时（毫秒）                                        ║
                  ║    - 实际方法调用次数（用于验证缓存命中率）                  ║
                  ║    - 缓存配置说明                                           ║
                  ╚══════════════════════════════════════════════════════════════╝
                  """);

app.Run();