﻿// <auto-generated />
using System;
using GAB.BatchServer.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace GAB.BatchServer.API.Migrations
{
    [DbContext(typeof(BatchServerContext))]
    partial class BatchServerContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.0-rtm-35687")
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("GAB.BatchServer.API.Models.Input", b =>
                {
                    b.Property<int>("InputId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("AssignedToLabUserId");

                    b.Property<Guid?>("BatchId");

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<DateTime>("ModifiedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<string>("Parameters")
                        .HasMaxLength(800);

                    b.Property<int>("Status");

                    b.HasKey("InputId");

                    b.HasIndex("AssignedToLabUserId");

                    b.HasIndex("BatchId")
                        .HasName("IDX_BatchId");

                    b.HasIndex("Status")
                        .HasName("IDX_Status");

                    b.ToTable("Inputs");
                });

            modelBuilder.Entity("GAB.BatchServer.API.Models.LabUser", b =>
                {
                    b.Property<int>("LabUserId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("CompanyName")
                        .HasMaxLength(50);

                    b.Property<string>("CountryCode")
                        .HasMaxLength(2);

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<string>("EMail")
                        .HasMaxLength(100);

                    b.Property<string>("FullName")
                        .HasMaxLength(50);

                    b.Property<string>("Location")
                        .HasMaxLength(50);

                    b.Property<DateTime>("ModifiedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<string>("TeamName")
                        .HasMaxLength(100);

                    b.HasKey("LabUserId");

                    b.HasIndex("EMail")
                        .IsUnique()
                        .HasName("IDX_Email")
                        .HasFilter("[EMail] IS NOT NULL");

                    b.ToTable("LabUsers");
                });

            modelBuilder.Entity("GAB.BatchServer.API.Models.Output", b =>
                {
                    b.Property<int>("OutputId")
                        .ValueGeneratedOnAdd()
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int>("CCD");

                    b.Property<int>("Camera");

                    b.Property<string>("ClientVersion")
                        .HasMaxLength(25);

                    b.Property<string>("ContainerId")
                        .HasMaxLength(256);

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<double>("Dec");

                    b.Property<string>("Frequencies");

                    b.Property<int?>("InputId");

                    b.Property<double>("IsNotPlanet");

                    b.Property<double>("IsPlanet");

                    b.Property<DateTime>("ModifiedOn")
                        .ValueGeneratedOnAdd()
                        .HasDefaultValueSql("getutcdate()");

                    b.Property<double>("RA");

                    b.Property<string>("Result")
                        .HasMaxLength(1024);

                    b.Property<int>("Sector");

                    b.Property<string>("TICId")
                        .HasMaxLength(20);

                    b.Property<double>("TMag");

                    b.HasKey("OutputId");

                    b.HasIndex("InputId");

                    b.ToTable("Outputs");
                });

            modelBuilder.Entity("GAB.BatchServer.API.Models.Input", b =>
                {
                    b.HasOne("GAB.BatchServer.API.Models.LabUser", "AssignedTo")
                        .WithMany()
                        .HasForeignKey("AssignedToLabUserId");
                });

            modelBuilder.Entity("GAB.BatchServer.API.Models.Output", b =>
                {
                    b.HasOne("GAB.BatchServer.API.Models.Input", "Input")
                        .WithMany()
                        .HasForeignKey("InputId");
                });
#pragma warning restore 612, 618
        }
    }
}
