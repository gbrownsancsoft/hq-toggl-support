﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HQ.Data.Models
{
    public class UserToken : IdentityUserToken<Guid>
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public class Map : IEntityTypeConfiguration<UserToken>
        {
            public virtual void Configure(EntityTypeBuilder<UserToken> builder)
            {
                builder.ToTable("user_tokens");
            }
        }
    }
}
