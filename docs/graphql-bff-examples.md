# GraphQL BFF — example queries

Endpoint: `POST /graphql`  
Header: `Authorization: Bearer <token>`

## Empresa con usuarios

```graphql
query CompanyWithUsers($id: UUID!) {
  companyById(id: $id) {
    id
    name
    status
    users {
      id
      email
      role
      isActive
    }
  }
}
```

## Empresa con documentos

```graphql
query CompanyWithDocuments($id: UUID!) {
  companyById(id: $id) {
    id
    name
    documents {
      id
      originalFileName
      status
      sizeBytes
      createdAt
    }
  }
}
```

## Tickets (con empresa y asignado)

```graphql
query TicketsOverview {
  tickets {
    id
    subject
    status
    priority
    company {
      id
      name
    }
    createdBy {
      id
      email
      role
    }
    assignedTo {
      id
      email
    }
  }
}
```

## Conversaciones con mensajes

```graphql
query ConversationsWithMessages {
  conversations {
    id
    title
    companyId
    messages {
      id
      role
      content
      createdAt
      sources {
        documentName
        similarity
      }
    }
  }
}
```

## Yo (perfil)

```graphql
query Me {
  me {
    userId
    email
    role
    companyId
    companyName
    isActive
  }
}
```
