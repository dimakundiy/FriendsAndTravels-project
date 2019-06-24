﻿using AutoMapper;
using FriendsAndTravel.Common.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FriendsAndTravel.BAL.Mapping
{
    public class AutoMapperProfile : Profile
    {
      

        private readonly string[] Assemblies = new[]
        {
            "FriendsAndTravel",
            "FriendsAndTravel.Data",
            "FriendsAndTravel.BAL",
            "FriendsAndTravel.Common"
        };

        public AutoMapperProfile()
        {
            var types = new List<Type>();

            foreach (var assemblyName in this.Assemblies)
            {
                types.AddRange(Assembly.Load(assemblyName).GetTypes());
            }

            types
                   .Where(t => t.IsClass
                   && !t.IsAbstract
                   && t.GetInterfaces().Where(i => i.IsGenericType).Select(i => i.GetGenericTypeDefinition()).Contains(typeof(IMapFrom<>)))
                   .Select(t => new
                   {
                       Source = t
                       .GetInterfaces()
                       .Where(i => i.IsGenericType)
                       .Select(i => new
                       {
                           Definition = i.GetGenericTypeDefinition(),
                           Arguments = i.GetGenericArguments()
                       })
                       .Where(i => i.Definition == typeof(IMapFrom<>))
                       .SelectMany(i => i.Arguments)
                       .First(),
                       Destination = t
                   })
                   .ToList()
                   .ForEach(m => this.CreateMap(m.Source, m.Destination));

            types
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IHaveCustomMapping).IsAssignableFrom(t))
                .Select(Activator.CreateInstance)
                .Cast<IHaveCustomMapping>()
                .ToList()
                .ForEach(m => m.ConfigureMapping(this));
        }
    }
}
