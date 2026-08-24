using BackendEjemplo.ManyToManyExample.Domain.Models;
using BackendEjemplo.ManyToManyExample.Resources;

namespace BackendEjemplo.ManyToManyExample.Mapping
{
    public static class CourseMappings
    {
        public static CourseResource ToResource(this Course c) => new()
        {
            Id = c.Id,
            Name = c.Name,
            Code = c.Code,
            Credits = c.Credits
        };

        public static Course ToEntity(this SaveCourseResource r) => new()
        {
            Name = r.Name,
            Code = r.Code,
            Credits = r.Credits!.Value
        };
    }
}
