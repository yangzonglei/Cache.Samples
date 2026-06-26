# Yzl.Net.Extensions.Cache.Samples

[![NuGet](https://img.shields.io/badge/NuGet-Yzl.Extensions.Cache-004880)](https://www.nuget.org/packages/Yzl.Extensions.Cache/)

**Yzl.Extensions.Cache** 是一个 .NET 的声明式缓存框架，灵感来自 Spring Cache。通过注解（Attribute）的方式，在方法上声明缓存策略，框架自动处理缓存的读写和过期逻辑，无需手动操作缓存 API。

本项目是该框架的**完整示例集合**，涵盖了框架的所有功能特性。每个示例都包含详细的代码注释、说明文档和可直接运行的 HTTP API 端点。

---

## 📋 项目结构

```
Yzl.Net.Extensions.Cache.Samples/
├── Controllers/
│   └── CacheSamplesController.cs    # 示例控制器（30+ 个端点）
├── Services/
│   ├── BasicCacheService.cs         # 第一章：基础 Cacheable 用法
│   ├── CacheLifecycleService.cs     # 第二章：CachePut & CacheEvict
│   ├── SpelKeyService.cs            # 第三章：SpEL 键表达式
│   ├── ConditionalService.cs        # 第四章：Condition & Unless
│   ├── ConfigInheritanceService.cs  # 第五章：CacheConfig 继承
│   ├── AsyncCacheService.cs         # 第六章：异步缓存
│   ├── SlidingExpirationService.cs  # 第七章：滑动过期
│   ├── RedisCacheService.cs         # 第八章：Redis 缓存
│   └── CacheEvictAllService.cs      # 第九章：CacheEvict AllEntries
├── Models/
│   ├── UserDto.cs                   # 用户模型
│   └── ProductDto.cs                # 产品模型
├── Program.cs                       # 应用入口 + DI 配置
├── appsettings.json                 # 配置文件
└── README.md                        # 本文档
```

---

## 🚀 快速开始

### 环境要求

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- （可选）[Redis](https://redis.io/) — 如需测试 Redis 缓存

### 运行项目

```bash
cd Yzl.Net.Extensions.Cache.Samples
dotnet run
```

启动后访问：**http://localhost:17005/api/samples**

首页会显示所有可用的示例端点，每个端点都可以点击测试。

### 启用 Redis

编辑 `appsettings.Development.json`，配置你的 Redis 连接字符串：

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,password=your_password"
  }
}
```

然后重启项目，控制台会显示 "Redis 缓存已启用"。

---

## 📖 详细功能介绍

---

### 第一章：基础 Cacheable 用法

`[Cacheable]` 是框架最核心的特性。当方法被标记为 `[Cacheable]` 时，框架会：

1. 根据 `key` 先去缓存中查找数据
2. **缓存命中** → 直接返回缓存数据，**跳过方法执行**
3. **缓存未命中** → 执行方法体，将返回值存入缓存后再返回

```csharp
[Cacheable(cacheName: "users", key: "#id", ttlSeconds: 60)]
public virtual UserDto? GetUser(int id)
{
    // 此方法只会在缓存未命中时执行
    return _db.Users.Find(id);
}
```

| 端点 | 说明 | 关键参数 |
|------|------|---------|
| `GET /api/samples/basic/{id}` | 基础缓存演示 | cacheName=users, ttlSeconds=60 |
| `GET /api/samples/basic/short-ttl/{id}` | 短 TTL 缓存 | ttlSeconds=10 |
| `GET /api/samples/basic/by-name?name=` | 字符串缓存键 | key=#name |
| `GET /api/samples/basic/age-range?minAge=&maxAge=` | 组合键缓存 | key=#minAge:#maxAge |

**核心参数：**

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `cacheName` | 缓存区域名称（类似于 Redis 的 key 前缀） | 使用方法全限定名 |
| `key` | 缓存键（支持 SpEL 表达式） | `""` |
| `ttlSeconds` | 过期时间（秒） | `300`（5分钟） |

**验证方法：** 同一端点连续请求两次，观察响应中的 `elapsedMs`：
- 第一次 → 约 1500ms（执行方法体）
- 第二次 → 约 0-5ms（缓存命中）

---

### 第二章：CachePut & CacheEvict（缓存生命周期管理）

完整的缓存生命周期包含三个操作：

| 注解 | 行为 | 适用场景 |
|------|------|---------|
| `[Cacheable]` | 缓存命中时跳过方法执行 | 查询操作 |
| `[CachePut]` | 始终执行方法，结果**始终写入缓存** | 更新操作 |
| `[CacheEvict]` | 从缓存中删除指定条目 | 删除操作 |

```csharp
[CachePut(cacheName: "lifecycle", key: "#user.Id")]
public virtual UserDto? UpdateUser(UserDto user)
{
    // 始终执行更新操作，并将结果写入缓存
    _db.Users.Update(user);
    return user;
}

[CacheEvict(cacheName: "lifecycle", key: "#id")]
public virtual void DeleteUser(int id)
{
    // 删除数据后清除缓存
    _db.Users.Remove(new UserDto { Id = id });
}
```

| 端点 | 说明 |
|------|------|
| `GET /api/samples/lifecycle/{id}` | 查询（Cacheable） |
| `POST /api/samples/lifecycle/update` | 更新（CachePut） |
| `POST /api/samples/lifecycle/delete` | 删除（CacheEvict） |
| `GET /api/samples/lifecycle/refresh/{id}` | 强制刷新（CachePut） |

**验证流程：**
1. `GET /api/samples/lifecycle/1` → 慢（首次查询）
2. `GET /api/samples/lifecycle/1` → 快（缓存命中）
3. `POST /api/samples/lifecycle/update` (id=1, name=NewName) → 更新并写入缓存
4. `GET /api/samples/lifecycle/1` → 快（读取到已更新的缓存数据）
5. `POST /api/samples/lifecycle/delete` (id=1) → 删除并驱逐缓存
6. `GET /api/samples/lifecycle/1` → 慢（缓存已清除，重新查询）

---

### 第三章：SpEL 表达式（缓存键语法）

缓存键支持类似 Spring 的 SpEL（Spring Expression Language）表达式语法，这是框架最强大的特性之一。

#### 语法总览

| 表达式 | 说明 | 示例 |
|--------|------|------|
| `#参数名` | 引用方法参数 | `#id`, `#name` |
| `#p0`, `#p1` | 按位置引用参数 | `#p0` = 第一个参数 |
| `#参数名.属性` | 嵌套属性访问 | `#user.Name`, `#qo.UserId` |
| `#参数名.属性1.属性2` | 多级嵌套 | `#order.User.Address.City` |
| `#字典名.key` | 字典键访问 | `#cfg.version` |
| 值1:值2 | 组合多个值 | `#qo.UserId:#qo.Keyword` |

#### 示例详解

```csharp
// 示例 1：嵌套属性访问
[Cacheable(cacheName: "spel:query",
           key: "#qo.UserId:#qo.Keyword")]
// 调用 GetUserByQuery(new QueryQo { UserId=1, Keyword="test" })
// 缓存键 → spel:query:1:test

// 示例 2：字典键访问
[Cacheable(cacheName: "spel:config",
           key: "#cfg.site_name:#cfg.version")]
// 调用 GetConfig(dict) 其中 dict["site_name"]="MyApp"
// 缓存键 → spel:config:MyApp:2.0.0

// 示例 3：位置参数
[Cacheable(cacheName: "spel:positional",
           key: "#p0")]
// #p0 等价于 #id，按位置引用

// 示例 4：省略 cacheName → 使用全限定方法名
[Cacheable(key: "#id")]
// cacheName = "Namespace.ClassName.MethodName"
```

| 端点 | 说明 |
|------|------|
| `GET /api/samples/spel/query?userId=&keyword=` | 嵌套属性 `#qo.UserId:#qo.Keyword` |
| `GET /api/samples/spel/config` | 字典键 `#cfg.site_name:#cfg.version` |
| `GET /api/samples/spel/positional/{id}` | 位置参数 `#p0` |
| `GET /api/samples/spel/default-name/{id}` | 默认方法全限定名 |

---

### 第四章：Condition & Unless（条件缓存控制）

框架提供了两个条件表达式来控制缓存行为，分别在**方法执行前后**评估。

#### Condition（前置条件）

- **评估时机：** 方法执行之前
- **可引用：** 方法参数（`#id`, `#user`, `#p0` 等）
- **true →** 正常使用缓存
- **false →** 跳过缓存，每次执行方法

```csharp
// 只缓存 id > 10 的数据
[Cacheable(cacheName: "users", key: "#id",
           Condition = "#id > 10")]
```

#### Unless（后置排除）

- **评估时机：** 方法执行之后
- **可引用：** 方法参数 + 返回值（`#result`）
- **true →** 结果不写入缓存
- **false →** 结果正常写入缓存

```csharp
// 不缓存 null 结果
[Cacheable(cacheName: "users", key: "#id",
           Unless = "#result == null")]
```

#### 组合使用

```csharp
// Condition + Unless 组合
[Cacheable(cacheName: "users", key: "#id",
           Condition = "#id > 0",
           Unless = "#result == null || #result.Age > 40")]
```

| 端点 | 说明 |
|------|------|
| `GET /api/samples/condition/cacheable/{id}` | Condition: `#id > 10` |
| `GET /api/samples/condition/unless/{id}` | Unless: `#result == null` |
| `GET /api/samples/condition/combined/{id}` | Condition + Unless 组合 |
| `GET /api/samples/condition/complex/{id}` | 复杂条件 |
| `POST /api/samples/condition/put` | CachePut + 条件 |

**验证方法：**
- 访问 `/api/samples/condition/cacheable/5`（id=5 ≤ 10，不缓存）→ 每次请求都执行方法
- 访问 `/api/samples/condition/cacheable/20`（id=20 > 10，缓存）→ 第一次执行，之后缓存命中

---

### 第五章：CacheConfig（类级配置继承）

`[CacheConfig]` 是类级别的注解，定义该类中所有缓存方法的默认配置。方法上的注解可以覆盖这些默认值。

#### 继承优先级

```
1. 方法上的 [Cacheable]/[CachePut] 显式指定值   ← 最高优先级
2. 类上的 [CacheConfig] 默认值                     ← 中间优先级
3. 特性构造函数的硬编码默认值                       ← 最低优先级
```

#### 可配置的默认属性

| CacheConfig 属性 | 对应方法级属性 | 说明 |
|------------------|--------------|------|
| `defaultCacheName` | `cacheName` | 默认缓存区域名称 |
| `defaultCacheType` | `cacheType` | 默认缓存类型 |
| `defaultTtlSeconds` | `ttlSeconds` | 默认 TTL |
| `defaultSlidingExpirationSeconds` | `slidingTtl` | 默认滑动过期 |

⚠️ **注意：`key` 不会被继承，每个方法必须显式指定 key。**

#### 示例

```csharp
[CacheConfig(defaultCacheName: "config-demo", defaultTtlSeconds: 120)]
public class ConfigInheritanceService
{
    // 完全继承：cacheName=config-demo, ttl=120s
    [Cacheable(key: "#id")]
    public virtual UserDto? GetDefault(int id) { ... }

    // 部分覆盖：cacheName=custom-name, ttl=120s（继承）
    [Cacheable(cacheName: "custom-name", key: "#id")]
    public virtual UserDto? GetWithCustomCacheName(int id) { ... }

    // 部分覆盖：cacheName=config-demo（继承）, ttl=30s
    [Cacheable(key: "#id", ttlSeconds: 30)]
    public virtual UserDto? GetWithCustomTtl(int id) { ... }

    // 完全覆盖：所有配置自己指定
    [Cacheable(cacheName: "fully-custom", key: "#id",
               ttlSeconds: 600, slidingTtl: 60)]
    public virtual UserDto? GetFullyCustom(int id) { ... }
}
```

| 端点 | 说明 |
|------|------|
| `GET /api/samples/config/default/{id}` | 完全继承 CacheConfig |
| `GET /api/samples/config/custom-name/{id}` | 覆盖 cacheName |
| `GET /api/samples/config/custom-ttl/{id}` | 覆盖 TTL |
| `GET /api/samples/config/fully-custom/{id}` | 完全覆盖 |

---

### 第六章：异步缓存

框架完全支持 async/await 异步方法的缓存。与同步方法的使用方式完全一致。

```csharp
// 异步方法与同步方法用法完全相同
[Cacheable(cacheName: "async:users", key: "#id", ttlSeconds: 60)]
public virtual async Task<UserDto?> GetUserAsync(int id)
{
    return await _db.Users.FindAsync(id);
}
```

| 端点 | 说明 |
|------|------|
| `GET /api/samples/async/{id}` | 异步查询 |
| `POST /api/samples/async/update` | 异步更新（CachePut） |
| `GET /api/samples/async/all` | 异步批量查询 |

**支持的特性（异步方法同样全部支持）：**
- ✅ CachePut / CacheEvict
- ✅ SpEL 表达式
- ✅ Condition / Unless
- ✅ 滑动过期
- ✅ Redis 后端

---

### 第七章：滑动过期（Sliding Expiration）

滑动过期是缓存系统中重要的过期策略。与固定 TTL 不同，滑动过期在每次访问缓存时自动续期。

| 策略 | 行为 | 适用场景 |
|------|------|---------|
| 固定 TTL | 无论是否被访问，到期一定过期 | 数据变化规律明确 |
| 滑动过期 | 每次访问自动续期，适用于"热点数据" | Session、Token、频繁访问的配置 |

```csharp
// 固定 TTL：10 秒后过期，无论是否被访问
[Cacheable(cacheName: "demo", key: "#id", ttlSeconds: 10)]

// 滑动过期：每次访问续期 30 秒，最长不超过 24 小时
[Cacheable(cacheName: "demo", key: "#id",
           ttlSeconds: 86400, slidingTtl: 30)]
```

⚠️ **当 `slidingTtl > 0` 时，`ttlSeconds` 退化为"绝对过期兜底"**，防止热点数据永远不过期。

| 端点 | 说明 |
|------|------|
| `GET /api/samples/sliding/fixed/{id}` | 固定 TTL（对照实验，10秒过期） |
| `GET /api/samples/sliding/basic/{id}` | 滑动过期（每次访问续期 30 秒） |

**验证方法（滑动过期）：**
1. `GET /api/samples/sliding/basic/1` → 慢（第一次查询）
2. 等待 20 秒（不超过 30 秒滑动窗口）
3. `GET /api/samples/sliding/basic/1` → 快（缓存命中，TTL 重置为 30 秒）
4. 等待 40 秒（超过 30 秒滑动窗口）
5. `GET /api/samples/sliding/basic/1` → 慢（缓存已过期）

---

### 第八章：Redis 缓存

框架支持 Memory 和 Redis 两种缓存后端，通过 `cacheType` 参数切换。同一个应用中还可以混合使用两种后端。

| 对比维度 | Memory（内存缓存） | Redis（分布式缓存） |
|---------|-------------------|-------------------|
| 存储位置 | 应用进程内存 | Redis 服务器 |
| 访问速度 | 微秒级 | 毫秒级 |
| 应用重启 | 缓存丢失 | 缓存保留 |
| 多实例共享 | ❌ 不共享 | ✅ 共享 |
| 适用场景 | 单实例、开发环境 | 多实例、生产环境 |

```csharp
// Redis 缓存
[Cacheable(cacheName: "redis:users", key: "#id",
           ttlSeconds: 300, cacheType: CacheType.Redis)]

// 同一服务中混合使用 Memory
[Cacheable(cacheName: "memory:users", key: "#id",
           ttlSeconds: 60, cacheType: CacheType.Memory)]
```

| 端点 | 说明 |
|------|------|
| `GET /api/samples/redis/{id}` | Redis 查询 |
| `GET /api/samples/redis/sliding/{id}` | Redis + 滑动过期 |
| `POST /api/samples/redis/update` | Redis CachePut |
| `GET /api/samples/redis/memory/{id}` | Memory 对照实验 |

**通过 redis-cli 验证：**
```bash
redis-cli
> keys redis:users:*       # 查看所有 Redis 缓存键
> get redis:users:1        # 查看具体缓存的 JSON 数据
> ttl redis:users:1        # 查看过期时间
```

---

### 第九章：CacheEvict AllEntries（批量清除缓存）

除了逐条清除外，`[CacheEvict]` 还支持 `allEntries = true` 批量清除整个缓存区域。

| 模式 | 说明 | 适用场景 |
|------|------|---------|
| 逐条清除 `key="#id"` | 只清除指定 key | 单个数据变更 |
| 批量清除 `allEntries=true` | 清除某区域下所有缓存 | 全量导入、定时刷新 |

```csharp
// 逐条清除
[CacheEvict(cacheName: "users", key: "#id")]

// 批量清除整个区域
[CacheEvict(cacheName: "users", allEntries: true)]
```

| 端点 | 说明 |
|------|------|
| `GET /api/samples/evict-all/{id}` | 查询（写入缓存） |
| `POST /api/samples/evict-all/evict-single/{id}` | 逐条清除 |
| `POST /api/samples/evict-all/clear-all` | 批量清除 |

---

## ⚙️ 配置参考

### NuGet 包

```xml
<PackageReference Include="Yzl.Extensions.Cache" Version="0.1.1" />
```

### AddEnableCaching 参数

```csharp
// 完整签名
public static IServiceCollection AddEnableCaching(
    this IServiceCollection services,
    Assembly[]? assemblies = null,      // 要扫描的程序集（null=自动）
    bool enableRedis = false,           // 是否启用 Redis
    string? redisConnectionString = null // Redis 连接字符串
)
```

### 服务注册要求

使用缓存注解的服务必须满足以下条件之一：

1. **方法为 `virtual`** — Castle DynamicProxy 通过继承创建代理类
2. **类实现了接口** — 通过接口代理

```csharp
// 方式 A：virtual 方法（推荐）
public class MyService
{
    [Cacheable(...)]
    public virtual UserDto? GetUser(int id) { ... }
}

// 方式 B：接口 + 实现类
public interface IMyService { UserDto? GetUser(int id); }
public class MyService : IMyService
{
    [Cacheable(...)]
    public UserDto? GetUser(int id) { ... }
}
```

### 特性参数速查表

| 特性 | cacheName | key | ttlSeconds | slidingTtl | cacheType | Condition | Unless | allEntries | ReadOnly |
|------|-----------|-----|-----------|-----------|-----------|-----------|--------|-----------|----------|
| `[Cacheable]` | ✅ 可选 | ✅ SpEL | ✅ 默认300s | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ |
| `[CachePut]` | ✅ 可选 | ✅ SpEL | ✅ 默认300s | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ |
| `[CacheEvict]` | ✅ **必需** | ✅ SpEL | ✅ 默认300s | ❌ | ✅ | ✅ | ❌ | ✅ | ❌ |
| `[CacheConfig]` | ✅ defaultName | ❌ | ✅ defaultTtl | ✅ defaultSliding | ✅ defaultType | ❌ | ❌ | ❌ | ❌ |

---

## 🧪 测试指南

### 使用 curl 测试

```bash
# 1. 基础缓存测试
curl http://localhost:17005/api/samples/basic/1

# 2. 缓存命中验证（连续请求两次，观察 elapsedMs）
curl http://localhost:17005/api/samples/basic/1
curl http://localhost:17005/api/samples/basic/1

# 3. 更新缓存
curl -X POST http://localhost:17005/api/samples/lifecycle/update \
  -d "id=1&name=NewName&age=25&email=new@test.com"

# 4. 删除缓存
curl -X POST http://localhost:17005/api/samples/lifecycle/delete -d "id=1"

# 5. SpEL 嵌套属性
curl "http://localhost:17005/api/samples/spel/query?userId=1&keyword=test"

# 6. Condition 条件缓存（id=5 不缓存，id=20 缓存）
curl http://localhost:17005/api/samples/condition/cacheable/5
curl http://localhost:17005/api/samples/condition/cacheable/20

# 7. Redis 缓存
curl http://localhost:17005/api/samples/redis/1

# 8. 查看统计
curl http://localhost:17005/api/samples/stats
```

### 使用浏览器测试

直接访问 http://localhost:17005/api/samples ，首页包含所有端点的链接，点击即可测试。

---

## 📚 参考资源

- [GitHub: Yzl.Extensions.Cache 源码](https://github.com/yangzonglei/dotnet-extensions)
- [NuGet: Yzl.Extensions.Cache](https://www.nuget.org/packages/Yzl.Extensions.Cache/)
- [Spring Cache 文档](https://docs.spring.io/spring-framework/docs/current/reference/html/integration.html#cache)（本框架的设计灵感来源）

---

## 📄 License

MIT
