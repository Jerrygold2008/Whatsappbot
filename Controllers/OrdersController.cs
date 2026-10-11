using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SellFlowApi.Dtos;
using SellFlowApi.Enums;
using SellFlowApi.Helpers;
using SellFlowApi.Interfaces;

namespace SellFlowApi.Controllers
{
    [ApiController]
    [Route("api/orders"), Authorize]
    public class OrdersController(IOrderService orders) : ControllerBase
    {
        [HttpGet, Authorize(Policy = "ActiveSubscription")]
        public async Task<ActionResult<List<OrderResponse>>> List([FromQuery] OrderStatus? status) =>
            Ok(await orders.ListAsync(User.GetBusinessId(), status));

        [HttpGet("{id:guid}"), Authorize(Policy = "ActiveSubscription")]
        public async Task<ActionResult<OrderResponse>> Get(Guid id)
        {
            var order = await orders.GetAsync(User.GetBusinessId(), id);
            return order is null ? NotFound() : Ok(order);
        }

        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, UpdateOrderStatusRequest request)
        {
            await orders.TransitionAsync(User.GetBusinessId(), id, request.Status, $"merchant:{User.GetUserId()}");
            return NoContent();
        }

        // --- NEW: Endpoints called by the Frontend OrderSheet ---

        [HttpGet("{id:guid}/proofs")]
        public async Task<ActionResult<List<PaymentProofResponse>>> Proofs(Guid id) =>
            Ok(await orders.ListProofsAsync(User.GetBusinessId(), id));

        [HttpGet("{id:guid}/proofs/{proofId:guid}")]
        public async Task<IActionResult> ProofFile(Guid id, Guid proofId)
        {
            var proof = await orders.GetProofAsync(User.GetBusinessId(), id, proofId);
            return File(proof.FileData, proof.ContentType);
        }

        [HttpPost("{id:guid}/payment/confirm")]
        public async Task<IActionResult> ConfirmPayment(Guid id)
        {
            await orders.ConfirmPaymentAsync(User.GetBusinessId(), id, $"merchant:{User.GetUserId()}");
            return NoContent();
        }

        [HttpPost("{id:guid}/payment/reject")]
        public async Task<IActionResult> RejectPayment(Guid id, RejectPaymentRequest request)
        {
            await orders.RejectPaymentAsync(User.GetBusinessId(), id, request.Reason, $"merchant:{User.GetUserId()}");
            return NoContent();
        }
    }
}
