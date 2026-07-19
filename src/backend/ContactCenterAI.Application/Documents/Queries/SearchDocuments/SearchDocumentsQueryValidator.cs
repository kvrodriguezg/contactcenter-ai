using FluentValidation;

namespace ContactCenterAI.Application.Documents.Queries.SearchDocuments;

public class SearchDocumentsQueryValidator : AbstractValidator<SearchDocumentsQuery>
{
    public SearchDocumentsQueryValidator()
    {
        RuleFor(query => query.Query)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(query => query.TopK)
            .InclusiveBetween(1, 20);
    }
}
