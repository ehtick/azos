﻿insert into tbl_nodelog
(
  `GDID`,
  `VERSION_UTC`,
  `VERSION_ORIGIN`,
  `VERSION_ACTOR`,
  `VERSION_STATE`,
  `G_NODE`,
  `G_PARENT`,
  `PATH_SEGMENT`,
  `START_UTC`,
  `PROPERTIES`,
  `CONFIG`
)
select
  @gdid,
  @version_utc,
  @version_origin,
  @version_actor,
  @version_state,
  @g_node,
  @g_parent,
  @psegment,
  @start_utc,
  @properties,
  @config
from
  tbl_node TN
where
  TN.GDID = @g_node