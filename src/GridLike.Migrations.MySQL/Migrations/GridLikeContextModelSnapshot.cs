﻿// <auto-generated />
using System;
using GridLike.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GridLike.Migrations
{
    [DbContext(typeof(GridLikeContext))]
    partial class GridLikeContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("GridLike.Data.Models.Job", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("char(36)");

                    b.Property<int>("BatchId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Ended")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("FailureCount")
                        .HasColumnType("int");

                    b.Property<Guid?>("FromId")
                        .HasColumnType("char(36)");

                    b.Property<string>("Key")
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)");

                    b.Property<int>("Priority")
                        .HasColumnType("int");

                    b.Property<DateTime>("Started")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<DateTime>("Submitted")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Type")
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)");

                    b.Property<int?>("WorkerId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Jobs");
                });

            modelBuilder.Entity("GridLike.Data.Models.JobBatch", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("JobType")
                        .HasColumnType("longtext");

                    b.Property<bool>("Stable")
                        .HasColumnType("tinyint(1)");

                    b.HasKey("Id");

                    b.ToTable("Batches");
                });

            modelBuilder.Entity("GridLike.Data.Models.WorkerRecord", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("UniqueId")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("UniqueId")
                        .IsUnique();

                    b.ToTable("Workers");
                });
#pragma warning restore 612, 618
        }
    }
}
