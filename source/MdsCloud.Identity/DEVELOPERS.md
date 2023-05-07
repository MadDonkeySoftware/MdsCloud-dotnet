# Developers

This document is intended to house guidelines for those contributing to the project.
The items here are to act as guidance material rather than steadfast rules.

## Project Structure

The project is broken out into a SOLID/Onion namespace structure to assist with organizing
code and keeping concerns separated. Given that these projects are meant to be micro-services
or near-micro-services some tradeoffs have been made in order to keep mental load on
developers / maintainers manageable.

Each project has a `Presentation`, `Core`, and `Infrastructure` namespace. Items in these areas
should adhere to a similar concerns.

### Presentation

The presentation layer handles user input and output concerns. This includes authentication,
authorization, first-pass input validation, and catch-all error handling. It is important to note
that validations done here are purely for ease of not taxing the Core layer.

### Core

The Core layer will seem like the most catch-all layer in the project. This layer is
responsible for items such as validation, coordinating calls to infrastructure, transaction
management, domain to/from data-transfer-object mappings, and other concerns. Generally
speaking this layer is the core application where all business rules and processing reside.
A developer should have reasonable confidence in business rules of the system if the a
method were tested at this layer in isolation of the Presentation or Infrastructure layers.
Additionally if a presentation or infrastructure implementation were to drastically change
there should be little to zero impact on the Core layer.

### Infrastructure

The infrastructure layer can also be though of as a repository layer. This layer is generally 
concerned with talking to out-of-process services. This is where communication with a 3rd party
API or a database would reside. Concerns such as translating a domain or data-transfer-object
into a request payload consumed by another system reside in this area.
