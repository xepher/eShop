// 全局命名空间引用定义，简化整个基础设施层项目的命名空间导入。

global using System.Data;
global using MediatR;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Design;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using Microsoft.EntityFrameworkCore.Storage;
global using eShop.Ordering.Domain.AggregatesModel.BuyerAggregate;
global using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
global using eShop.Ordering.Domain.Exceptions;
global using eShop.Ordering.Domain.Seedwork;
global using eShop.Ordering.Infrastructure.EntityConfigurations;
global using eShop.Ordering.Infrastructure.Idempotency;
