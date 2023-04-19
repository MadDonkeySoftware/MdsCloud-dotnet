# Developers

This document is intended to house guidelines for those contributing to the project.
The items here are to act as guidance material rather than steadfast rules.

## Project Structure

The project is broken out into a N-tier-like namespace structure to assist with organizing
code and keeping concerns separated. Given that these projects are meant to be micro-services
or near-micro-services some tradeoffs have been made in order to keep mental load on
developers / maintainers manageable.

Each project has a `Presentation`, `Business`, `Infrastructure`, and optionally a `Domain`
namespace. Items in these areas should adhere to a similar concerns.

### Presentation

The presentation layer handles user input and output concerns. This includes authentication,
authorization, first-pass input validation, and catch-all error handling.

### Business

The business layer will seem like the most catch-all layer in the project. This layer is
responsible for items such as next-pass input validation, coordinating calls to infrastructure,
transaction management, domain to/from data-transfer-object mappings, and other concerns. Generally
speaking a developer would have reasonable confidence in business rules of the system if the a
method were tested at this layer in isolation of the Presentation or Infrastructure layers.

### Infrastructure

The infrastructure layer can also be though of as a repository layer. This layer is generally 
concerned with talking to out-of-process services. This is where communication with a 3rd party
API or a database would reside. Concerns such as translating a domain or data-transfer-object
into a request payload consumed by another system reside in this area.

### Domain

The domain layer will generally be object definitions devoid of functionality. If one were using
a functional programming paradigm these objects would represent the input and output data that
various other methods would use. In the context of the greater project these objects represent
the model that the software operates against.