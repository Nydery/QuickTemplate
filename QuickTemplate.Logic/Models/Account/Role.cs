﻿//@BaseCode
//MdStart

namespace QuickTemplate.Logic.Models.Account
{
    public class Role
    {
        public string Designation { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public static Role Create(Entities.Account.Role role)
        {
            var result = new Role();

            result.CopyFrom(role);
            return result;
        }
    }
}
//MdEnd