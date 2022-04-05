using BlueprintAPI.DbContexts;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BlueprintAPI.Models.Repositories {
    public class Repository<T> where T : class {
        public BlueprintsDbContext BlueprintsContext { get; }

        public Repository(BlueprintsDbContext blueprintsContext) {
            BlueprintsContext = blueprintsContext;
        }




        //CREATE
        public virtual void Create(T type) {
            if (type == null) {
                return;
            }

            BlueprintsContext.Add(type);
        }




        //READ
        public virtual async ValueTask<T> GetIDAsync(Guid id, CancellationToken cancellationToken = default) {
            return await BlueprintsContext.FindAsync<T>(new object[] { id }, cancellationToken: cancellationToken);
        }

        public virtual async ValueTask<T> GetAsync(T type, CancellationToken cancellationToken = default) {
            return await GetAsync(x => x == type, cancellationToken);
        }

        public virtual async ValueTask<T> GetAsync(Func<T, bool> function, CancellationToken cancellationToken = default) {
            await foreach (T type in GetAllAsync(function, cancellationToken)) {
                return type;
            }

            return null;
        }

        public virtual async IAsyncEnumerable<T> GetAllAsync(Func<T, bool> function, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            await foreach (T type in BlueprintsContext.Set<T>()) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }

                if (function(type)) {
                    yield return type;
                }
            }
        }




        //UPDATE
        public virtual void Update(T type) {
            BlueprintsContext.Update(type);
        }




        //DELETE
        public virtual void Delete(T type) {
            BlueprintsContext.Remove(type);
        }




        //SAVE
        public virtual async Task SaveAsync(CancellationToken cancellationToken = default) {
            await BlueprintsContext.SaveChangesAsync(cancellationToken);
        }
    }
}
