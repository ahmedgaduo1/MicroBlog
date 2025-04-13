global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;

global using MicroBlog.Domain.Entities;
global using MicroBlog.Application.Common.Interfaces;
global using MicroBlog.Infrastructure.Data;
global using MicroBlog.Web.Services;
global using MicroBlog.Web.Controllers;
global using MicroBlog.Web.Models;

global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.EntityFrameworkCore;

global using Xunit;
global using Moq;
global using FluentAssertions;
global using System.Net;
global using System.Net.Http.Json;
