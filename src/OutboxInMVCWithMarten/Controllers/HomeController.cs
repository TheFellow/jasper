using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Jasper.Marten;
using Jasper.Messaging;
using Marten;
using Microsoft.AspNetCore.Mvc;
using TestMessages;

namespace OutboxInMVCWithMarten.Controllers
{
    public class HomeController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }

        // SAMPLE: using-outbox-with-marten-in-mvc-action
        [HttpPost]
        public async Task<IActionResult> CreateUser(
            string userId,
            [FromServices] IDocumentStore martenStore,
            [FromServices] IMessageContext context)
        {
            // The Marten IDocumentSession represents the unit of work
            using (var session = martenStore.OpenSession())
            {
                // This directs the current message context
                // to persist outgoing messages with this
                // Marten session.
                await context.EnlistInTransaction(session);

                var theUser = new User { Id = userId };
                session.Store(theUser);

                await context.Send(new NewUser {UserId = userId});

                // The outgoing messages will be persisted
                // and sent to the outgoing transports
                // as a result of the transaction succeeding here
                await session.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
        }
        // ENDSAMPLE








        [HttpPost]
        public async Task<IActionResult> DeleteUser(
            string userId,
            [FromServices] IDocumentStore martenStore,
            [FromServices] IMessageContext bus)
        {
            // the bus can use a document session no matter how it has been created
            using (var session = martenStore.DirtyTrackedSession(IsolationLevel.Serializable))
            {
                await bus.EnlistInTransaction(session);

                var existing = session.Load<User>(userId);
                if (existing != null && !existing.IsDeleted)
                {
                    existing.IsDeleted = true;
                    await bus.Publish(new UserDeleted { UserId = userId });
                    await session.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class User
    {
        public string Id { get; set; }
        public bool IsDeleted { get; set; }
    }
}
