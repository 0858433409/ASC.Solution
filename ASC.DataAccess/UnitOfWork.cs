using ASC.DataAccess.Interfaces;

using ASC.Model.BaseTypes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace ASC.DataAccess
{
    public class UnitOfWork : IUnitOfWork, IDisposable
    {
        private readonly DbContext _dbContext;
        private readonly Dictionary<Type, object> _repositories;

        public UnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext;
            _repositories = new Dictionary<Type, object>();
        }

        public int CommitTransaction()
        {
            return _dbContext.SaveChanges();
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dbContext.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IRepository<T> Repository<T>() where T : BaseEntity
        {
            var type = typeof(T);
            if (_repositories.ContainsKey(type)) return (IRepository<T>)_repositories[type];

            var repositoryInstance = Activator.CreateInstance(typeof(Repository<>).MakeGenericType(type), _dbContext);
            _repositories.Add(type, repositoryInstance);
            return (IRepository<T>)_repositories[type];
        }
    }
}