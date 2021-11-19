﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Random;
using PetImages.Entities;
using PetImages.Storage;
using PetImages.Tests.Exceptions;

namespace PetImages.Tests.StorageMocks
{
    public class MockCosmosContainer : ICosmosContainer
    {
        private readonly string ContainerName;
        private readonly MockCosmosState State;
        private readonly Generator Generator;
        private bool EmitRandomizedFaults;
        private object SyncObject;

        public MockCosmosContainer(string containerName, MockCosmosState state)
        {
            this.ContainerName = containerName;
            this.State = state;
            this.Generator = Generator.Create();
            this.EmitRandomizedFaults = false;
            this.SyncObject = new object();
        }

        public Task<T> CreateItem<T>(T item)
            where T : DbItem
        {
            lock (this.SyncObject)
            {
                if (this.EmitRandomizedFaults && this.Generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                System.Console.WriteLine($"[{System.Threading.Thread.CurrentThread.ManagedThreadId}] trying to create inner ...");
                var itemCopy = TestHelper.Clone(item);
                this.State.CreateItem(this.ContainerName, itemCopy);
                return Task.FromResult(itemCopy);
            }
        }

        public Task<T> GetItem<T>(string partitionKey, string id)
            where T : DbItem
        {
            lock (this.SyncObject)
            {
                if (this.EmitRandomizedFaults && this.Generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                var item = this.State.GetItem(this.ContainerName, partitionKey, id);
                var itemCopy = TestHelper.Clone((T)item);
                return Task.FromResult(itemCopy);
            }
        }

        public Task<T> UpsertItem<T>(T item)
            where T : DbItem
        {
            lock (this.SyncObject)
            {
                if (this.EmitRandomizedFaults && this.Generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                var itemCopy = TestHelper.Clone(item);
                this.State.UpsertItem(this.ContainerName, itemCopy);
                return Task.FromResult(itemCopy);
            }
        }

        public Task DeleteItem(string partitionKey, string id)
        {
            lock (this.SyncObject)
            {
                if (this.EmitRandomizedFaults && this.Generator.NextBoolean())
                {
                    throw new SimulatedDatabaseFaultException();
                }

                this.State.DeleteItem(this.ContainerName, partitionKey, id);
                return Task.CompletedTask;
            }
        }

        public void EnableRandomizedFaults()
        {
            this.EmitRandomizedFaults = true;
        }

        public void DisableRandomizedFaults()
        {
            this.EmitRandomizedFaults = false;
        }

    }
}
