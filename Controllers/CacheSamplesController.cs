using Microsoft.AspNetCore.Mvc;
using Yzl.Net.Extensions.Cache.Samples.Models;
using Yzl.Net.Extensions.Cache.Samples.Services;

namespace Yzl.Net.Extensions.Cache.Samples.Controllers;

/// <summary>
/// 缓存示例主控制器
///
/// 每个端点对应一个具体的缓存使用场景，
/// 建议配合 README.md 和 Service 中的 XML 注释一起阅读。
/// </summary>
[ApiController]
[Route("api/samples")]
public class CacheSamplesController : ControllerBase
{
    private readonly BasicCacheService _basicCache;
    private readonly CacheLifecycleService _lifecycle;
    private readonly SpelKeyService _spelKey;
    private readonly ConditionalService _conditional;
    private readonly ConfigInheritanceService _config;
    private readonly AsyncCacheService _async;
    private readonly SlidingExpirationService _sliding;
    private readonly RedisCacheService _redis;
    private readonly CacheEvictAllService _evictAll;

    public CacheSamplesController(
        BasicCacheService basicCache,
        CacheLifecycleService lifecycle,
        SpelKeyService spelKey,
        ConditionalService conditional,
        ConfigInheritanceService config,
        AsyncCacheService async,
        SlidingExpirationService sliding,
        RedisCacheService redis,
        CacheEvictAllService evictAll)
    {
        _basicCache = basicCache;
        _lifecycle = lifecycle;
        _spelKey = spelKey;
        _conditional = conditional;
        _config = config;
        _async = async;
        _sliding = sliding;
        _redis = redis;
        _evictAll = evictAll;
    }

    // ===================================================================
    // 首页
    // ===================================================================

    /// <summary>
    /// 示例导航首页
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return Content(GetHomePage(), "text/html");
    }

    // ===================================================================
    // 第一章：基础 Cacheable 用法
    // ===================================================================

    /// <summary>
    /// 【1.1】基础缓存 — 按 ID 查询用户
    /// </summary>
    [HttpGet("basic/{id}")]
    public IActionResult GetUser(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _basicCache.GetUser(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _basicCache.GetUserCallCount,
            message = user != null
                ? $"第 {_basicCache.GetUserCallCount} 次实际调用（非缓存命中）。耗时：{sw.ElapsedMilliseconds}ms"
                : "用户不存在",
            cacheKey = $"users:{id}",
            ttl = "60秒（固定TTL）"
        });
    }

    /// <summary>
    /// 【1.2】短 TTL 缓存（10 秒过期）
    /// </summary>
    [HttpGet("basic/short-ttl/{id}")]
    public IActionResult GetUserShortTtl(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _basicCache.GetUserShortTtl(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _basicCache.GetUserCallCount,
            ttl = "10秒",
            note = "10秒内反复请求会命中缓存；10秒后缓存过期，再次执行方法体"
        });
    }

    /// <summary>
    /// 【1.3】按名称查询用户（字符串缓存键）
    /// </summary>
    [HttpGet("basic/by-name")]
    public IActionResult GetUserByName([FromQuery] string name)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _basicCache.GetUserByName(name);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _basicCache.GetUserCallCount,
            cacheKey = $"users:name:{name}",
            note = "字符串作为缓存键，不区分大小写"
        });
    }

    /// <summary>
    /// 【1.4】按年龄范围查询（组合键）
    /// </summary>
    [HttpGet("basic/age-range")]
    public IActionResult GetUsersByAgeRange([FromQuery] int minAge, [FromQuery] int maxAge)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var users = _basicCache.GetUsersByAgeRange(minAge, maxAge);
        sw.Stop();
        return Ok(new
        {
            data = users,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _basicCache.GetUserCallCount,
            cacheKey = $"users:age-range:{minAge}:{maxAge}",
            note = "多参数组合键，不同参数组合对应不同缓存"
        });
    }

    // ===================================================================
    // 第二章：CachePut 和 CacheEvict（缓存生命周期）
    // ===================================================================

    /// <summary>
    /// 【2.1】查询用户（Cacheable）
    /// </summary>
    [HttpGet("lifecycle/{id}")]
    public IActionResult LifecycleGet(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _lifecycle.GetUser(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _lifecycle.CallCount,
            operation = "Cacheable（查询）"
        });
    }

    /// <summary>
    /// 【2.2】更新用户（CachePut）— 方法始终执行，结果写入缓存
    /// </summary>
    [HttpPost("lifecycle/update")]
    public IActionResult LifecycleUpdate([FromForm] int id, [FromForm] string name,
        [FromForm] int age, [FromForm] string email)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _lifecycle.UpdateUser(new UserDto
        {
            Id = id, Name = name, Age = age, Email = email
        });
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _lifecycle.CallCount,
            operation = "CachePut（更新并写入缓存）",
            note = "CachePut 始终执行方法体，并将结果写入缓存"
        });
    }

    /// <summary>
    /// 【2.3】删除用户（CacheEvict）
    /// </summary>
    [HttpPost("lifecycle/delete")]
    public IActionResult LifecycleDelete([FromForm] int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _lifecycle.DeleteUser(id);
        sw.Stop();
        return Ok(new
        {
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _lifecycle.CallCount,
            operation = "CacheEvict（删除并清除缓存）",
            clearedCacheKey = $"lifecycle:{id}",
            note = "从数据源删除数据，同时驱逐缓存中的对应条目"
        });
    }

    /// <summary>
    /// 【2.4】刷新缓存（CachePut + 重新加载）
    /// </summary>
    [HttpGet("lifecycle/refresh/{id}")]
    public IActionResult LifecycleRefresh(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _lifecycle.RefreshUser(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _lifecycle.CallCount,
            operation = "CachePut（强制刷新缓存）",
            note = "从数据库重新加载数据并更新缓存"
        });
    }

    // ===================================================================
    // 第三章：SpEL 表达式
    // ===================================================================

    /// <summary>
    /// 【3.1】SpEL 嵌套属性访问
    /// </summary>
    [HttpGet("spel/query")]
    public IActionResult SpelQuery([FromQuery] int userId, [FromQuery] string keyword)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _spelKey.GetUserByQuery(new QueryQo { UserId = userId, Keyword = keyword });
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _spelKey.CallCount,
            cacheKey = $"spel:query:{userId}:{keyword}",
            spelExpression = "#qo.UserId:#qo.Keyword"
        });
    }

    /// <summary>
    /// 【3.2】SpEL 字典键访问
    /// </summary>
    [HttpGet("spel/config")]
    public IActionResult SpelConfig()
    {
        var cfg = new Dictionary<string, object>
        {
            ["site_name"] = "MyApp",
            ["version"] = "2.0.0"
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var config = _spelKey.GetConfig(cfg);
        sw.Stop();
        return Ok(new
        {
            data = config,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _spelKey.CallCount,
            cacheKey = $"spel:config:MyApp:2.0.0",
            spelExpression = "#cfg.site_name:#cfg.version"
        });
    }

    /// <summary>
    /// 【3.3】SpEL 位置参数 #p0, #p1
    /// </summary>
    [HttpGet("spel/positional/{id}")]
    public IActionResult SpelPositional(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _spelKey.GetByIdPositional(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _spelKey.CallCount,
            spelExpression = "#p0（位置参数，等价于 #id）"
        });
    }

    /// <summary>
    /// 【3.5】SpEL 默认方法名作为 cacheName
    /// </summary>
    [HttpGet("spel/default-name/{id}")]
    public IActionResult SpelDefaultName(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _spelKey.GetUserByDefaultName(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _spelKey.CallCount,
            cacheName = "自动使用方法全限定名",
            note = "未指定 cacheName 时，框架会使用 '命名空间.类名.方法名' 作为缓存区域"
        });
    }

    // ===================================================================
    // 第四章：Condition 和 Unless
    // ===================================================================

    /// <summary>
    /// 【4.1】Condition — 仅 id > 10 时缓存
    /// </summary>
    [HttpGet("condition/cacheable/{id}")]
    public IActionResult ConditionCacheable(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _conditional.GetUserWithCondition(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _conditional.ConditionCallCount,
            condition = "#id > 10",
            isCaching = id > 10,
            note = id > 10
                ? "Condition=true → 正常使用缓存"
                : "Condition=false → 每次调用都执行方法（不缓存）"
        });
    }

    /// <summary>
    /// 【4.2】Unless — 排除 null 结果
    /// </summary>
    [HttpGet("condition/unless/{id}")]
    public IActionResult ConditionUnless(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _conditional.GetUserWithUnless(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _conditional.UnlessCallCount,
            unless = "#result == null",
            cached = user != null,
            note = user != null ? "结果不为 null → 写入缓存" : "结果为 null → 不缓存"
        });
    }

    /// <summary>
    /// 【4.3】Condition + Unless 组合
    /// </summary>
    [HttpGet("condition/combined/{id}")]
    public IActionResult ConditionCombined(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _conditional.GetUserCombined(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _conditional.CombinedCallCount,
            condition = "#id > 0",
            unless = "#result == null || #result.Age > 40",
            shouldCache = user != null && user.Age <= 40,
            note = user != null
                ? (user.Age <= 40 ? "条件满足 → 缓存" : "年龄 > 40 → 不缓存")
                : "结果为空 → 不缓存"
        });
    }

    /// <summary>
    /// 【4.6】复杂条件
    /// </summary>
    [HttpGet("condition/complex/{id}")]
    public IActionResult ConditionComplex(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _conditional.GetUserComplex(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _conditional.ComplexCallCount,
            condition = "#p0 > 0 && #p0 < 100",
            unless = "#result.Email == 'skip@test.com'",
            note = user?.Email == "skip@test.com"
                ? "除非条件满足（skip邮箱）→ 不缓存"
                : "条件全部通过 → 正常缓存"
        });
    }

    /// <summary>
    /// 【4.4】CachePut 条件写入
    /// </summary>
    [HttpPost("condition/put")]
    public IActionResult ConditionPut([FromForm] int id, [FromForm] string name,
        [FromForm] int age)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _conditional.ConditionalUpdateUser(new UserDto
        {
            Id = id, Name = name, Age = age, Email = $"{name}@test.com"
        });
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _conditional.PutCallCount,
            note = name == "skip" ? "名称为 'skip' → 不缓存" : "正常写入缓存"
        });
    }

    // ===================================================================
    // 第五章：CacheConfig 继承
    // ===================================================================

    /// <summary>
    /// 【5.1】完全继承 CacheConfig
    /// </summary>
    [HttpGet("config/default/{id}")]
    public IActionResult ConfigDefault(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _config.GetDefault(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _config.CallCount,
            inheritedCacheName = "config-demo（从 [CacheConfig] 继承）",
            inheritedTtl = "120秒（从 [CacheConfig] 继承）"
        });
    }

    /// <summary>
    /// 【5.2】部分覆盖 — 自定义 cacheName
    /// </summary>
    [HttpGet("config/custom-name/{id}")]
    public IActionResult ConfigCustomName(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _config.GetWithCustomCacheName(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _config.CallCount,
            cacheName = "custom-name（覆盖）",
            ttl = "120秒（继承）"
        });
    }

    /// <summary>
    /// 【5.3】部分覆盖 — 自定义 TTL
    /// </summary>
    [HttpGet("config/custom-ttl/{id}")]
    public IActionResult ConfigCustomTtl(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _config.GetWithCustomTtl(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _config.CallCount,
            cacheName = "config-demo（继承）",
            ttl = "30秒（覆盖默认值 120秒）"
        });
    }

    /// <summary>
    /// 【5.4】完全覆盖
    /// </summary>
    [HttpGet("config/fully-custom/{id}")]
    public IActionResult ConfigFullyCustom(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _config.GetFullyCustom(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _config.CallCount,
            cacheName = "fully-custom",
            ttl = "600秒",
            slidingTtl = "60秒",
            note = "方法级别配置完全覆盖了 [CacheConfig] 默认值"
        });
    }

    // ===================================================================
    // 第六章：异步缓存
    // ===================================================================

    /// <summary>
    /// 【6.1】异步查询用户
    /// </summary>
    [HttpGet("async/{id}")]
    public async Task<IActionResult> AsyncGet(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = await _async.GetUserAsync(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _async.CallCount,
            operation = "异步 Cacheable",
            note = "异步方法同样支持缓存，使用方式与同步方法完全一致"
        });
    }

    /// <summary>
    /// 【6.2】异步更新用户
    /// </summary>
    [HttpPost("async/update")]
    public async Task<IActionResult> AsyncUpdate([FromForm] int id, [FromForm] string name,
        [FromForm] int age, [FromForm] string email)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = await _async.UpdateUserAsync(new UserDto
        {
            Id = id, Name = name, Age = age, Email = email
        });
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _async.CallCount,
            operation = "异步 CachePut"
        });
    }

    /// <summary>
    /// 【6.5】异步获取全部用户
    /// </summary>
    [HttpGet("async/all")]
    public async Task<IActionResult> AsyncGetAll()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var users = await _async.GetAllUsersAsync();
        sw.Stop();
        return Ok(new
        {
            data = users,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _async.CallCount,
            operation = "异步批量查询（缓存集合）",
            note = "返回 List<T> 的异步方法同样支持缓存"
        });
    }

    // ===================================================================
    // 第七章：滑动过期
    // ===================================================================

    /// <summary>
    /// 【7.1】固定 TTL — 对照实验
    /// </summary>
    [HttpGet("sliding/fixed/{id}")]
    public IActionResult SlidingFixed(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _sliding.GetUserFixed(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _sliding.CallCount,
            ttlMode = "固定 TTL（10秒绝对过期）",
            note = "无论是否被访问，10秒后一定过期"
        });
    }

    /// <summary>
    /// 【7.2】滑动过期 — 每次访问续期 30 秒
    /// </summary>
    [HttpGet("sliding/basic/{id}")]
    public IActionResult SlidingBasic(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _sliding.GetUserSliding(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _sliding.CallCount,
            ttlMode = "滑动过期",
            slidingTtl = "30秒（每次访问续期）",
            absoluteMaxTtl = "86400秒（24小时绝对上限）",
            note = "每次访问缓存时，TTL 重置为 30 秒；超过 30 秒未访问则过期"
        });
    }

    // ===================================================================
    // 第八章：Redis 缓存
    // ===================================================================

    /// <summary>
    /// 【8.1】Redis 查询用户
    /// </summary>
    [HttpGet("redis/{id}")]
    public IActionResult RedisGet(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _redis.GetUser(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _redis.CallCount,
            cacheType = "Redis",
            cacheKey = $"redis:users:{id}",
            note = "数据存储在 Redis 中，多实例共享缓存。可通过 redis-cli 查看"
        });
    }

    /// <summary>
    /// 【8.2】Redis + 滑动过期
    /// </summary>
    [HttpGet("redis/sliding/{id}")]
    public IActionResult RedisSliding(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _redis.GetUserSliding(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _redis.CallCount,
            cacheType = "Redis + 滑动过期",
            slidingTtl = "300秒",
            absoluteMaxTtl = "86400秒（24小时）"
        });
    }

    /// <summary>
    /// 【8.3】Redis CachePut
    /// </summary>
    [HttpPost("redis/update")]
    public IActionResult RedisUpdate([FromForm] int id, [FromForm] string name,
        [FromForm] int age)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _redis.UpdateUser(new UserDto
        {
            Id = id, Name = name, Age = age, Email = $"{name}@redis.com"
        });
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            cacheType = "Redis CachePut",
            note = "更新数据库后同步更新 Redis 缓存，多实例共享最新数据"
        });
    }

    /// <summary>
    /// 【8.5】Memory 缓存（与 Redis 对比）
    /// </summary>
    [HttpGet("redis/memory/{id}")]
    public IActionResult RedisMemory(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _redis.GetUserFromMemory(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _redis.CallCount,
            cacheType = "Memory（进程内缓存）",
            note = "数据存储在应用程序进程内存中，访问速度最快但多实例不共享"
        });
    }

    // ===================================================================
    // 第九章：CacheEvict AllEntries
    // ===================================================================

    /// <summary>
    /// 【9.1】查询用户（用于演示缓存清除）
    /// </summary>
    [HttpGet("evict-all/{id}")]
    public IActionResult EvictAllGet(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var user = _evictAll.GetUser(id);
        sw.Stop();
        return Ok(new
        {
            data = user,
            elapsedMs = sw.ElapsedMilliseconds,
            callCount = _evictAll.CallCount,
            note = $"用户 {id} 的数据已缓存到 'evict-all' 区域"
        });
    }

    /// <summary>
    /// 【9.2】逐条清除缓存（仅清除指定 ID）
    /// </summary>
    [HttpPost("evict-all/evict-single/{id}")]
    public IActionResult EvictSingle(int id)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _evictAll.EvictSingle(id);
        sw.Stop();
        return Ok(new
        {
            elapsedMs = sw.ElapsedMilliseconds,
            operation = $"已清除 evict-all:{id} 的缓存",
            note = "只清除了指定用户的缓存，其他用户的缓存仍然有效"
        });
    }

    /// <summary>
    /// 【9.3】批量清除整个缓存区域
    /// </summary>
    [HttpPost("evict-all/clear-all")]
    public IActionResult EvictAll()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _evictAll.EvictAll();
        sw.Stop();
        return Ok(new
        {
            elapsedMs = sw.ElapsedMilliseconds,
            operation = "已清除 'evict-all' 区域下所有缓存",
            note = "下次查询任何用户时，都将重新从数据源加载"
        });
    }

    // ===================================================================
    // 实用工具：查看服务调用统计
    // ===================================================================

    /// <summary>
    /// 查看所有服务的调用统计
    /// </summary>
    [HttpGet("stats")]
    public IActionResult Stats()
    {
        return Ok(new
        {
            basicCache_calls = _basicCache.GetUserCallCount,
            lifecycle_calls = _lifecycle.CallCount,
            spelKey_calls = _spelKey.CallCount,
            conditional_calls = new
            {
                condition = _conditional.ConditionCallCount,
                unless = _conditional.UnlessCallCount,
                combined = _conditional.CombinedCallCount,
                complex = _conditional.ComplexCallCount,
                put = _conditional.PutCallCount,
            },
            config_calls = _config.CallCount,
            async_calls = _async.CallCount,
            sliding_calls = _sliding.CallCount,
            redis_calls = _redis.CallCount,
            evictAll_calls = _evictAll.CallCount,
            totalActualMethodCalls = _basicCache.GetUserCallCount
                + _lifecycle.CallCount
                + _spelKey.CallCount
                + _conditional.ConditionCallCount
                + _conditional.UnlessCallCount
                + _conditional.CombinedCallCount
                + _conditional.ComplexCallCount
                + _conditional.PutCallCount
                + _config.CallCount
                + _async.CallCount
                + _sliding.CallCount
                + _redis.CallCount
                + _evictAll.CallCount,
            tip = "调用统计计数的是实际执行的方法次数（不含缓存命中的请求），用于验证缓存命中率"
        });
    }

    // ===================================================================
    // 首页 HTML
    // ===================================================================
    private string GetHomePage()
    {
        return @"<!DOCTYPE html>
<html lang='zh-CN'>
<head>
    <meta charset='UTF-8'>
    <title>Yzl.Net.Extensions.Cache 示例项目</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: -apple-system, 'Microsoft YaHei', sans-serif; background: #f5f5f5; padding: 20px; color: #333; }
        .container { max-width: 1000px; margin: 0 auto; }
        h1 { color: #2563eb; border-bottom: 3px solid #2563eb; padding-bottom: 10px; margin-bottom: 20px; font-size: 24px; }
        h2 { color: #1e40af; margin: 25px 0 10px; padding-left: 10px; border-left: 4px solid #2563eb; font-size: 18px; }
        .section { background: #fff; border-radius: 8px; padding: 15px 20px; margin-bottom: 15px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
        .section h3 { color: #374151; margin-bottom: 8px; font-size: 15px; }
        .section p { color: #6b7280; font-size: 13px; margin-bottom: 10px; line-height: 1.6; }
        .endpoint { display: inline-block; background: #059669; color: #fff; padding: 3px 8px; border-radius: 4px; font-size: 12px; font-family: monospace; margin: 2px 0; }
        .endpoint.get { background: #059669; }
        .endpoint.post { background: #d97706; }
        ul { list-style: none; padding: 0; }
        li { padding: 6px 0; border-bottom: 1px solid #f0f0f0; display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
        li:last-child { border-bottom: none; }
        a { color: #2563eb; text-decoration: none; font-size: 14px; font-family: monospace; }
        a:hover { text-decoration: underline; color: #1d4ed8; }
        .badge { font-size: 11px; padding: 2px 6px; border-radius: 10px; color: #fff; }
        .badge.memory { background: #6366f1; }
        .badge.redis { background: #ef4444; }
        .badge.both { background: #8b5cf6; }
        .badge.async { background: #0ea5e9; }
        .desc { font-size: 12px; color: #9ca3af; margin-left: 4px; }
        .stats-link { display: inline-block; margin-top: 15px; padding: 8px 16px; background: #2563eb; color: #fff; border-radius: 6px; text-decoration: none; font-size: 14px; }
        .stats-link:hover { background: #1d4ed8; }
    </style>
</head>
<body>
<div class='container'>
    <h1>🚀 Yzl.Net.Extensions.Cache 示例项目</h1>
    <p style='color:#6b7280;margin-bottom:20px;'>
        本项目演示了 Yzl.Extensions.Cache 框架的所有核心功能。
        建议按照章节顺序依次访问，配合源码中的 XML 注释学习。
        <br/>
        <strong>GitHub:</strong> <a href='https://github.com/yangzonglei/Cache.Samples' target='_blank'>https://github.com/yangzonglei/Cache.Samples</a>
    </p>

    <h2>📖 第一章：基础 Cacheable 用法</h2>
    <div class='section'>
        <ul>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/basic/1'>/api/samples/basic/{id}</a> <span class='desc'>基础缓存（按ID查询，TTL=60s）</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/basic/short-ttl/1'>/api/samples/basic/short-ttl/{id}</a> <span class='desc'>短 TTL（10秒过期）</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/basic/by-name?name=Alice'>/api/samples/basic/by-name?name=</a> <span class='desc'>字符串缓存键</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/basic/age-range?minAge=25&maxAge=35'>/api/samples/basic/age-range?minAge=&maxAge=</a> <span class='desc'>组合键缓存</span></li>
        </ul>
    </div>

    <h2>📖 第二章：CachePut &amp; CacheEvict（缓存生命周期）</h2>
    <div class='section'>
        <ul>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/lifecycle/1'>/api/samples/lifecycle/{id}</a> <span class='desc'>查询（Cacheable）</span></li>
            <li><span class='endpoint post'>POST</span> <a href='/api/samples/lifecycle/update'>/api/samples/lifecycle/update</a> <span class='desc'>更新（CachePut，始终执行并写入）</span></li>
            <li><span class='endpoint post'>POST</span> <a href='/api/samples/lifecycle/delete'>/api/samples/lifecycle/delete</a> <span class='desc'>删除（CacheEvict）</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/lifecycle/refresh/1'>/api/samples/lifecycle/refresh/{id}</a> <span class='desc'>强制刷新缓存（CachePut）</span></li>
        </ul>
    </div>

    <h2>📖 第三章：SpEL 键表达式</h2>
    <div class='section'>
        <ul>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/spel/query?userId=1&keyword=test'>/api/samples/spel/query</a> <span class='desc'>嵌套属性访问 #qo.UserId:#qo.Keyword</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/spel/config'>/api/samples/spel/config</a> <span class='desc'>字典键访问 #cfg.site_name:#cfg.version</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/spel/positional/1'>/api/samples/spel/positional/{id}</a> <span class='desc'>位置参数 #p0</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/spel/default-name/1'>/api/samples/spel/default-name/{id}</a> <span class='desc'>默认方法全限定名作为 cacheName</span></li>
        </ul>
    </div>

    <h2>📖 第四章：Condition &amp; Unless（条件缓存）</h2>
    <div class='section'>
        <ul>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/condition/cacheable/20'>/api/samples/condition/cacheable/{id}</a> <span class='desc'>Condition: #id > 10</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/condition/unless/1'>/api/samples/condition/unless/{id}</a> <span class='desc'>Unless: #result == null</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/condition/combined/1'>/api/samples/condition/combined/{id}</a> <span class='desc'>Condition + Unless 组合</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/condition/complex/1'>/api/samples/condition/complex/{id}</a> <span class='desc'>复杂条件：#p0 && 排除特殊邮箱</span></li>
            <li><span class='endpoint post'>POST</span> /api/samples/condition/put <span class='desc'>CachePut + 条件写入</span></li>
        </ul>
    </div>

    <h2>📖 第五章：CacheConfig 继承</h2>
    <div class='section'>
        <ul>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/config/default/1'>/api/samples/config/default/{id}</a> <span class='desc'>完全继承 [CacheConfig]</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/config/custom-name/10'>/api/samples/config/custom-name/{id}</a> <span class='desc'>覆盖 cacheName</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/config/custom-ttl/1'>/api/samples/config/custom-ttl/{id}</a> <span class='desc'>覆盖 TTL</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/config/fully-custom/1'>/api/samples/config/fully-custom/{id}</a> <span class='desc'>完全覆盖</span></li>
        </ul>
    </div>

    <h2>📖 第六章：异步缓存</h2>
    <div class='section'>
        <ul>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/async/100'>/api/samples/async/{id}</a> <span class='desc'>异步查询（async/await）</span></li>
            <li><span class='endpoint post'>POST</span> /api/samples/async/update <span class='desc'>异步更新（CachePut）</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/async/all'>/api/samples/async/all</a> <span class='desc'>异步批量查询（缓存集合）</span></li>
        </ul>
    </div>

    <h2>📖 第七章：滑动过期</h2>
    <div class='section'>
        <ul>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/sliding/fixed/1'>/api/samples/sliding/fixed/{id}</a> <span class='desc'>固定 TTL（对照实验）</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/sliding/basic/1'>/api/samples/sliding/basic/{id}</a> <span class='desc'>滑动过期（每次访问续期 30 秒）</span></li>
        </ul>
    </div>

    <h2>📖 第八章：Redis 缓存</h2>
    <div class='section'>
        <ul>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/redis/1'>/api/samples/redis/{id}</a> <span class='desc'>Redis 查询</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/redis/sliding/1'>/api/samples/redis/sliding/{id}</a> <span class='desc'>Redis + 滑动过期</span></li>
            <li><span class='endpoint post'>POST</span> /api/samples/redis/update <span class='desc'>Redis CachePut</span></li>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/redis/memory/1'>/api/samples/redis/memory/{id}</a> <span class='desc'>Memory vs Redis 对比</span></li>
        </ul>
    </div>

    <h2>📖 第九章：CacheEvict AllEntries</h2>
    <div class='section'>
        <ul>
            <li><span class='endpoint get'>GET</span> <a href='/api/samples/evict-all/1'>/api/samples/evict-all/{id}</a> <span class='desc'>查询用户（写入缓存）</span></li>
            <li><span class='endpoint post'>POST</span> /api/samples/evict-all/evict-single/{id} <span class='desc'>逐条清除缓存</span></li>
            <li><span class='endpoint post'>POST</span> /api/samples/evict-all/clear-all <span class='desc'>批量清除整个缓存区域</span></li>
        </ul>
    </div>

    <div style='text-align:center;margin-top:20px;'>
        <a href='/api/samples/stats' class='stats-link'>📊 查看服务调用统计</a>
    </div>

    <p style='text-align:center;color:#9ca3af;font-size:12px;margin-top:30px;'>
        Yzl.Net.Extensions.Cache.Samples &copy; 2024 | NuGet: Yzl.Extensions.Cache v0.1.1
    </p>
</div>
</body>
</html>";
    }
}
