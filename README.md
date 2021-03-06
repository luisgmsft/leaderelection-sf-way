# The case for Leader Election with Azure Service Fabric

Leader Election pattern fits well for node based scenarios (where a node maps to a VM), but Service Fabric (SF) offers enough infrastructure not to have to even think about election-disconnection-reelection-and-so -on.

Let’s use a Scale-Out scenario for comparison: we’ve been asked to support a distributed application that has to process a data workload, aggregating a result. We may use 1 to N nodes to process batches.

1. **Traditional Leader Election**. We’ll need 2 logical roles: Leader (for coordination and result gathering) and Worker (for algorithmic data processing and result passing). Both of these roles, coexist under each member. The colony elects (not truly, as it comes to whoever claims the title first) a Leader each time the throne gets headless. So, it’s auto-regulated, resilient and available. Every member is two-faced and knows how to play the election game each time required, as well as follow rules and work when not in throne.

2. **A Service Fabric approach**. Although we may try to replicate this into SF. It offers a new way of thinking: splitting roles into 1 Leader and N Workers. Leadership can be achieved with an Stateful Reliable Service (which is resilient, replicated and available always by leader-election and some other techniques already abstracted by the platform itself). In English, if the Leader falls down, the platform takes care of breeding another, feeding it with the knowledge (state) its predecessor had). Workers (stateless, stateful, actor, guests or external to-the-cluster client) will always talk to the Leader in command at the time, being properly routed by the SF Naming Service (kinda like a gateway).

---

## Sample implementation details
[To-Be-Completed]

* Leader
* Worker
* Workload, splitting
* Taking down the leader so reelection occurs
