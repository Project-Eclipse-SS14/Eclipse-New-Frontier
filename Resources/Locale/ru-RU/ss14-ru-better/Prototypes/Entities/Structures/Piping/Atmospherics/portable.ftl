# HASH: ae12295c5f1ad155430132591ad2faf93dd3e13712d74c8a87bc0d587014c7a1
ent-PortableScrubber = portable scrubber
    .desc = It scrubs, portably!
# HASH: 5340c75abf6d67bc817fafe48c3607f535bb1e430fed977d5ba194565bd82876
ent-SpaceHeater = space heater
    .desc = A bluespace technology device that alters local temperature. Commonly referred to as a "Space Heater".
    .suffix = Unanchored
# HASH: 39e93558958beeb5f291c58867060d954deab00b0fcde128932fb97eb2cc5a8c
ent-SpaceHeaterAnchored = { ent-SpaceHeater }
    .desc = { ent-SpaceHeater.desc }
    .suffix = Anchored
# HASH: d76a3b16b91dd7e9da4ce38414f6d2a58389bc917efc560231636a242ead0e31
ent-SpaceHeaterEnabled = { ent-SpaceHeaterAnchored }
    .desc = { ent-SpaceHeaterAnchored.desc }
    .suffix = Anchored, Enabled
