using AutoService.DAL;
using AutoService.Models;

namespace AutoService.BLL;

public class EmployeeService
{
    private readonly IRepository<Employee> _employees;

    public EmployeeService(IRepository<Employee> employees)
    {
        _employees = employees;
    }

    public List<Employee> GetAll() => _employees.GetAll();

    public Employee Add(string fullName, string specialization)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Введите ФИО механика.");

        var employee = new Employee
        {
            FullName = fullName.Trim(),
            Position = "Механик",
            Specialization = specialization.Trim()
        };
        _employees.Add(employee);
        return employee;
    }
}