using Hospital.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Hospital.Infrastructure.Identity
{
    public class DatabaseSeeder
    {
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;

        public DatabaseSeeder(RoleManager<IdentityRole<Guid>> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            await SeedRolesAsync();
        }

        private async Task SeedRolesAsync()
        {
            // Use reflection to get all the string constants in Roles.cs
            // We only want the individual role constants, not the composite ones (like AdminAndAbove)
            var roleType = typeof(Roles);
            var fields = roleType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            foreach (var field in fields)
            {
                if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                {
                    var roleName = (string)field.GetRawConstantValue()!;
                    
                    // Skip the composite strings which contain commas
                    if (roleName.Contains(","))
                        continue;

                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                    }
                }
            }
        }
    }
}
