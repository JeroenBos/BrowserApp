using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BrowserApp.Controllers
{
    [Route("api/[controller]")]
    public class ChangesController : Controller
    {
        [HttpPost("[action]")]
        public async Task<object> RegisterRequest()
        {
            await Task.Delay(1000);
            return new Response(new PropertyChange() { Id = 0, PropertyName = "number", Value = 1 });
        }


        new class Response
        {
            public Change[] Changes { get; }
            public Response(params Change[] changes)
            {
                if (changes == null) throw new ArgumentNullException(nameof(changes));

                this.Changes = changes;
            }
        }
        class Change
        {
            public int Id { get; set; }
        }

        class PropertyChange : Change
        {
            public string PropertyName { get; set; }
            public object Value { get; set; }
        }
        class CollectionChange : Change
        {
            public string CollectionName { get; set; }
        }
        class ICollectionItemRemoved : CollectionChange
        {
            public int RemovedItemId { get; set; }
        }
        class CollectionItemAdded : CollectionChange
        {
            public object Item { get; set; }
            public int? Index { get; set; }
        }
        class ICollectionItemsReordered : CollectionChange
        {
            public int Index1 { get; set; }
            public int Index2 { get; set; }
        }
    }
}