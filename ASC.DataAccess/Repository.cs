using ASC.DataAccess.Interfaces;
using ASC.Model.BaseType;
using ASC.Model.BaseTypes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

public class Repository<T> : IRepository<T> where T : BaseEntity, new()
{
    private readonly DbContext _dbContext;

    public Repository(DbContext dbContext)
    {
        this._dbContext = dbContext;
    }

    public async Task<T> AddAsync(T entity)
    {
        entity.CreatedDate = DateTime.UtcNow;
        await _dbContext.Set<T>().AddAsync(entity);
        return entity;
    }

    public void Update(T entity)
    {
        _dbContext.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
    }

    public async Task<T> FindAsync(string partitionKey, string rowKey)
    {
        return await _dbContext.Set<T>().FindAsync(partitionKey, rowKey);
    }

    public async Task<IEnumerable<T>> FindAllByPartitionKeyAsync(string partitionKey)
    {
        var result = await _dbContext.Set<T>().Where(e => e.PartitionKey == partitionKey).ToListAsync();
        return result;
    }

    public async Task<IEnumerable<T>> FindAllAsync()
    {
        return await _dbContext.Set<T>().ToListAsync();
    }
    public async Task<IEnumerable<T>> FindAllByQuery(Expression<Func<T, bool>> filter)
    {
        var result = _dbContext.Set<T>().Where(filter).ToListAsync().Result;
        return result as IEnumerable<T>;
    }

    public Task<IEnumerable<T>> FindAllInAuditByQuery(Expression<Func<T, bool>> filter)
    {
        throw new NotImplementedException();
    }
}

