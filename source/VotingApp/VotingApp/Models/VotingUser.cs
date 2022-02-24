﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VotingApp.Models
{
    [Table("VotingUser")]
    public partial class VotingUser
    {
        public VotingUser()
        {
            CreatedVotes = new HashSet<CreatedVote>();
            SubmittedVotes = new HashSet<SubmittedVote>();
        }

        [Key]
        [Column("ID")]
        [StringLength(100)]
        public string Id { get; set; } = null!;
        [StringLength(250)]
        public string UserName { get; set; } = null!;

        [InverseProperty(nameof(CreatedVote.User))]
        public virtual ICollection<CreatedVote> CreatedVotes { get; set; }
        [InverseProperty(nameof(SubmittedVote.User))]
        public virtual ICollection<SubmittedVote> SubmittedVotes { get; set; }
    }
}
