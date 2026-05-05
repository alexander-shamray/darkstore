using FluentValidation;

using JetBrains.Annotations;

namespace DarkStore.Application.Orders.Commands.PlaceOrder;

[UsedImplicitly]
public sealed class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId must be a non-empty GUID.");

        RuleFor(x => x.Street)
            .NotEmpty().WithMessage("Delivery street is required.")
            .MaximumLength(200);

        RuleFor(x => x.Building)
            .NotEmpty().WithMessage("Building number is required.")
            .MaximumLength(20);

        RuleFor(x => x.Apartment)
            .MaximumLength(20)
            .When(x => x.Apartment is not null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90m, 90m).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180m, 180m).WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.DisplayLabel)
            .NotEmpty().WithMessage("Display label is required.")
            .MaximumLength(500);

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("An order must contain at least one item.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Each item must have a valid ProductId.");
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Item quantity must be at least 1.")
                .LessThanOrEqualTo(999).WithMessage("Item quantity cannot exceed 999.");
            item.RuleFor(i => i.UnitPrice)
                .GreaterThan(0).WithMessage("Unit price must be greater than 0.");
        });

        RuleFor(x => x.LoyaltyPointsToRedeem)
            .GreaterThanOrEqualTo(0).WithMessage("Loyalty points to redeem cannot be negative.");
    }
}

