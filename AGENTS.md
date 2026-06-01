# Architecture Rules

Always strictly* follow these architecture rules:

* One responsibility per unit. **(Single Responsibility Principle — SRP)**
* One active context/state model. **(Bounded Context / Single Active Model)**
* Minimize knowledge between components. **(Law of Demeter / Principle of Least Knowledge / Low Coupling)**
* Use abstractions to hide implementation details. **(Abstraction / Encapsulation / Information Hiding)**
* Separate state-changing actions from read-only queries. **(Command Query Separation — CQS / Command Query Responsibility Segregation — CQRS)**
* Validate inputs and handle failures safely. **(Defensive Programming / Fail Fast / Fail Safe)**
* Make features extensible without rewriting stable code. **(Open/Closed Principle — OCP)**
* Protect core logic from unnecessary change. **(Clean Architecture / Hexagonal Architecture / Ports and Adapters / Dependency Rule)**
* The most CRITICAL rule: Keep the design simple. Keep it simple stupid. **(KISS / YAGNI)**
* Use one source of truth for shared data and rules. **(Single Source of Truth — SSOT / DRY)**

* Strictly following a rule means not following it must be considered a red line and a deal breaker.
