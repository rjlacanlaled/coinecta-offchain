﻿// <auto-generated />
using System.Text.Json;
using Coinecta.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Coinecta.Data.Migrations
{
    [DbContext(typeof(CoinectaDbContext))]
    [Migration("20240305143351_UtxoByAddressNullableTxOutCbor")]
    partial class UtxoByAddressNullableTxOutCbor
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("coinecta")
                .HasAnnotation("ProductVersion", "8.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Cardano.Sync.Data.Models.Block", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<decimal>("Number")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id", "Number", "Slot");

                    b.HasIndex("Slot");

                    b.ToTable("Blocks", "coinecta");
                });

            modelBuilder.Entity("Cardano.Sync.Data.Models.ReducerState", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Hash")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Name");

                    b.ToTable("ReducerStates", "coinecta");
                });

            modelBuilder.Entity("Cardano.Sync.Data.Models.TransactionOutput", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<long>("Index")
                        .HasColumnType("bigint");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.HasKey("Id", "Index");

                    b.HasIndex("Slot");

                    b.ToTable("TransactionOutputs", "coinecta");
                });

            modelBuilder.Entity("Coinecta.Data.Models.Reducers.StakePoolByAddress", b =>
                {
                    b.Property<string>("Address")
                        .HasColumnType("text");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("TxHash")
                        .HasColumnType("text");

                    b.Property<decimal>("TxIndex")
                        .HasColumnType("numeric(20,0)");

                    b.Property<JsonElement>("StakePoolJson")
                        .HasColumnType("jsonb");

                    b.HasKey("Address", "Slot", "TxHash", "TxIndex");

                    b.ToTable("StakePoolByAddresses", "coinecta");
                });

            modelBuilder.Entity("Coinecta.Data.Models.Reducers.StakePositionByStakeKey", b =>
                {
                    b.Property<string>("StakeKey")
                        .HasColumnType("text");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("TxHash")
                        .HasColumnType("text");

                    b.Property<decimal>("TxIndex")
                        .HasColumnType("numeric(20,0)");

                    b.Property<decimal>("LockTime")
                        .HasColumnType("numeric(20,0)");

                    b.Property<JsonElement>("StakePositionJson")
                        .HasColumnType("jsonb");

                    b.HasKey("StakeKey", "Slot", "TxHash", "TxIndex");

                    b.ToTable("StakePositionByStakeKeys", "coinecta");
                });

            modelBuilder.Entity("Coinecta.Data.Models.Reducers.StakeRequestByAddress", b =>
                {
                    b.Property<string>("Address")
                        .HasColumnType("text");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("TxHash")
                        .HasColumnType("text");

                    b.Property<decimal>("TxIndex")
                        .HasColumnType("numeric(20,0)");

                    b.Property<JsonElement>("StakePoolJson")
                        .HasColumnType("jsonb");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.HasKey("Address", "Slot", "TxHash", "TxIndex");

                    b.ToTable("StakeRequestByAddresses", "coinecta");
                });

            modelBuilder.Entity("Coinecta.Data.Models.Reducers.UtxoByAddress", b =>
                {
                    b.Property<string>("Address")
                        .HasColumnType("text");

                    b.Property<decimal>("Slot")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("TxHash")
                        .HasColumnType("text");

                    b.Property<decimal>("TxIndex")
                        .HasColumnType("numeric(20,0)");

                    b.Property<int>("Status")
                        .HasColumnType("integer");

                    b.Property<byte[]>("TxOutCbor")
                        .HasColumnType("bytea");

                    b.HasKey("Address", "Slot", "TxHash", "TxIndex", "Status");

                    b.ToTable("UtxosByAddress", "coinecta");
                });

            modelBuilder.Entity("Cardano.Sync.Data.Models.TransactionOutput", b =>
                {
                    b.OwnsOne("Cardano.Sync.Data.Models.Datum", "Datum", b1 =>
                        {
                            b1.Property<string>("TransactionOutputId")
                                .HasColumnType("text");

                            b1.Property<long>("TransactionOutputIndex")
                                .HasColumnType("bigint");

                            b1.Property<byte[]>("Data")
                                .IsRequired()
                                .HasColumnType("bytea");

                            b1.Property<int>("Type")
                                .HasColumnType("integer");

                            b1.HasKey("TransactionOutputId", "TransactionOutputIndex");

                            b1.ToTable("TransactionOutputs", "coinecta");

                            b1.WithOwner()
                                .HasForeignKey("TransactionOutputId", "TransactionOutputIndex");
                        });

                    b.OwnsOne("Cardano.Sync.Data.Models.Value", "Amount", b1 =>
                        {
                            b1.Property<string>("TransactionOutputId")
                                .HasColumnType("text");

                            b1.Property<long>("TransactionOutputIndex")
                                .HasColumnType("bigint");

                            b1.Property<decimal>("Coin")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<JsonElement>("MultiAssetJson")
                                .HasColumnType("jsonb");

                            b1.HasKey("TransactionOutputId", "TransactionOutputIndex");

                            b1.ToTable("TransactionOutputs", "coinecta");

                            b1.WithOwner()
                                .HasForeignKey("TransactionOutputId", "TransactionOutputIndex");
                        });

                    b.Navigation("Amount")
                        .IsRequired();

                    b.Navigation("Datum");
                });

            modelBuilder.Entity("Coinecta.Data.Models.Reducers.StakePoolByAddress", b =>
                {
                    b.OwnsOne("Cardano.Sync.Data.Models.Value", "Amount", b1 =>
                        {
                            b1.Property<string>("StakePoolByAddressAddress")
                                .HasColumnType("text");

                            b1.Property<decimal>("StakePoolByAddressSlot")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<string>("StakePoolByAddressTxHash")
                                .HasColumnType("text");

                            b1.Property<decimal>("StakePoolByAddressTxIndex")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<decimal>("Coin")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<JsonElement>("MultiAssetJson")
                                .HasColumnType("jsonb");

                            b1.HasKey("StakePoolByAddressAddress", "StakePoolByAddressSlot", "StakePoolByAddressTxHash", "StakePoolByAddressTxIndex");

                            b1.ToTable("StakePoolByAddresses", "coinecta");

                            b1.WithOwner()
                                .HasForeignKey("StakePoolByAddressAddress", "StakePoolByAddressSlot", "StakePoolByAddressTxHash", "StakePoolByAddressTxIndex");
                        });

                    b.Navigation("Amount")
                        .IsRequired();
                });

            modelBuilder.Entity("Coinecta.Data.Models.Reducers.StakePositionByStakeKey", b =>
                {
                    b.OwnsOne("Cardano.Sync.Data.Models.Datums.Rational", "Interest", b1 =>
                        {
                            b1.Property<string>("StakePositionByStakeKeyStakeKey")
                                .HasColumnType("text");

                            b1.Property<decimal>("StakePositionByStakeKeySlot")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<string>("StakePositionByStakeKeyTxHash")
                                .HasColumnType("text");

                            b1.Property<decimal>("StakePositionByStakeKeyTxIndex")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<decimal>("Denominator")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<decimal>("Numerator")
                                .HasColumnType("numeric(20,0)");

                            b1.HasKey("StakePositionByStakeKeyStakeKey", "StakePositionByStakeKeySlot", "StakePositionByStakeKeyTxHash", "StakePositionByStakeKeyTxIndex");

                            b1.ToTable("StakePositionByStakeKeys", "coinecta");

                            b1.WithOwner()
                                .HasForeignKey("StakePositionByStakeKeyStakeKey", "StakePositionByStakeKeySlot", "StakePositionByStakeKeyTxHash", "StakePositionByStakeKeyTxIndex");
                        });

                    b.OwnsOne("Cardano.Sync.Data.Models.Value", "Amount", b1 =>
                        {
                            b1.Property<string>("StakePositionByStakeKeyStakeKey")
                                .HasColumnType("text");

                            b1.Property<decimal>("StakePositionByStakeKeySlot")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<string>("StakePositionByStakeKeyTxHash")
                                .HasColumnType("text");

                            b1.Property<decimal>("StakePositionByStakeKeyTxIndex")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<decimal>("Coin")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<JsonElement>("MultiAssetJson")
                                .HasColumnType("jsonb");

                            b1.HasKey("StakePositionByStakeKeyStakeKey", "StakePositionByStakeKeySlot", "StakePositionByStakeKeyTxHash", "StakePositionByStakeKeyTxIndex");

                            b1.ToTable("StakePositionByStakeKeys", "coinecta");

                            b1.WithOwner()
                                .HasForeignKey("StakePositionByStakeKeyStakeKey", "StakePositionByStakeKeySlot", "StakePositionByStakeKeyTxHash", "StakePositionByStakeKeyTxIndex");
                        });

                    b.Navigation("Amount")
                        .IsRequired();

                    b.Navigation("Interest")
                        .IsRequired();
                });

            modelBuilder.Entity("Coinecta.Data.Models.Reducers.StakeRequestByAddress", b =>
                {
                    b.OwnsOne("Cardano.Sync.Data.Models.Value", "Amount", b1 =>
                        {
                            b1.Property<string>("StakeRequestByAddressAddress")
                                .HasColumnType("text");

                            b1.Property<decimal>("StakeRequestByAddressSlot")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<string>("StakeRequestByAddressTxHash")
                                .HasColumnType("text");

                            b1.Property<decimal>("StakeRequestByAddressTxIndex")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<decimal>("Coin")
                                .HasColumnType("numeric(20,0)");

                            b1.Property<JsonElement>("MultiAssetJson")
                                .HasColumnType("jsonb");

                            b1.HasKey("StakeRequestByAddressAddress", "StakeRequestByAddressSlot", "StakeRequestByAddressTxHash", "StakeRequestByAddressTxIndex");

                            b1.ToTable("StakeRequestByAddresses", "coinecta");

                            b1.WithOwner()
                                .HasForeignKey("StakeRequestByAddressAddress", "StakeRequestByAddressSlot", "StakeRequestByAddressTxHash", "StakeRequestByAddressTxIndex");
                        });

                    b.Navigation("Amount")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
