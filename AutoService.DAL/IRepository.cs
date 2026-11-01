using AutoService.Models;

namespace AutoService.DAL;

public interface IRepository<T> where T : IEntity
{
    List<T> GetAll();
    T? GetById(int id);
    void Add(T item);
    void Update(T item);
    void Delete(int id);
}